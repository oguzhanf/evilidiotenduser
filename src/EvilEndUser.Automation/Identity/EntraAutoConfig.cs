using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Azure.Identity.Broker;
using EvilEndUser.Core.Models;
using Microsoft.Extensions.Logging;

namespace EvilEndUser.Automation.Identity;

/// <summary>The signed-in Entra identity discovered from the OS account.</summary>
public sealed record EntraIdentity(string? Upn, string? TenantId);

/// <summary>A discovered Azure AI Foundry / Azure OpenAI chat deployment.</summary>
public sealed record FoundryTarget(string ResourceName, string Endpoint, string Deployment, string ModelName);

/// <summary>The result of a zero-config auto-detection pass.</summary>
public sealed record AutoConfigResult(TokenCredential Credential, EntraIdentity? Identity, FoundryTarget? Foundry)
{
    /// <summary>True when a usable Foundry deployment was found.</summary>
    public bool HasFoundry => Foundry is not null;
}

/// <summary>
/// Zero-configuration provisioning for Entra-joined machines. Uses the Windows WAM broker to sign in
/// <b>silently</b> as the machine's signed-in Entra account (no device codes, no copy-paste), then
/// auto-discovers the user's identity/tenant and any Azure AI Foundry / Azure OpenAI chat deployment
/// they can access — all keyless, using the same Entra identity for data-plane calls.
/// </summary>
public sealed class EntraAutoConfig
{
    private static readonly string[] ChatModelPreference =
    {
        "gpt-4o", "gpt-4.1", "gpt-4o-mini", "gpt-4.1-mini", "o4-mini", "o3", "gpt-4", "gpt-35-turbo", "gpt-3.5-turbo",
    };

    private readonly ILogger? _logger;

    /// <summary>Creates the auto-config service.</summary>
    public EntraAutoConfig(ILogger? logger = null) => _logger = logger;

    /// <summary>
    /// Builds a broker (WAM) credential bound to the app window. With
    /// <c>UseDefaultBrokerAccount</c> it uses the OS-signed-in Entra account silently, only showing
    /// the native broker dialog if silent SSO is not possible.
    /// </summary>
    public static TokenCredential CreateBrokerCredential(nint windowHandle, string? tenantId = null)
        => new InteractiveBrowserCredential(new InteractiveBrowserCredentialBrokerOptions(windowHandle)
        {
            UseDefaultBrokerAccount = true,
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId,
        });

    /// <summary>
    /// Runs a full zero-config pass: creates the broker credential, discovers identity/tenant, and
    /// discovers a Foundry/Azure OpenAI chat deployment, writing everything into <paramref name="config"/>.
    /// </summary>
    public async Task<AutoConfigResult> AutoConfigureAsync(
        SimulationConfig config, nint windowHandle, CancellationToken ct = default)
    {
        var credential = CreateBrokerCredential(windowHandle, config.Llm.TenantId);

        var identity = await TryDiscoverIdentityAsync(credential, ct).ConfigureAwait(false);
        if (identity is not null)
        {
            if (string.IsNullOrWhiteSpace(config.SelfMailbox)) config.SelfMailbox = identity.Upn;
            if (string.IsNullOrWhiteSpace(config.Llm.TenantId)) config.Llm.TenantId = identity.TenantId;
        }

        var foundry = await TryDiscoverFoundryAsync(credential, ct).ConfigureAwait(false);
        if (foundry is not null)
        {
            config.Llm.Provider = LlmProvider.AzureOpenAI;
            config.Llm.Endpoint = foundry.Endpoint;
            config.Llm.DeploymentName = foundry.Deployment;
            config.Llm.ApiKey = null;              // keyless
            config.Llm.UseEntraIdentity = true;
            config.Llm.AutoDiscovered = true;
        }

        return new AutoConfigResult(credential, identity, foundry);
    }

    /// <summary>Discovers the signed-in UPN + tenant by decoding a Graph access token (offline, no call).</summary>
    public async Task<EntraIdentity?> TryDiscoverIdentityAsync(TokenCredential credential, CancellationToken ct = default)
    {
        try
        {
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }), ct).ConfigureAwait(false);

            var claims = DecodeJwtClaims(token.Token);
            var upn = Claim(claims, "upn") ?? Claim(claims, "preferred_username") ?? Claim(claims, "email");
            var tid = Claim(claims, "tid");
            _logger?.LogInformation("Discovered Entra identity {Upn} (tenant {Tid})", upn, tid);
            return new EntraIdentity(upn, tid);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Entra identity discovery failed");
            return null;
        }
    }

    /// <summary>
    /// Enumerates the Azure subscriptions the user can access, finds Cognitive Services / Azure AI
    /// Foundry accounts (kinds OpenAI or AIServices), lists their chat-model deployments, and returns
    /// the best match (preferring the newest GPT-4o-class model).
    /// </summary>
    public async Task<FoundryTarget?> TryDiscoverFoundryAsync(TokenCredential credential, CancellationToken ct = default)
    {
        try
        {
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://management.azure.com/.default" }), ct).ConfigureAwait(false);

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.Authorization = new("Bearer", token.Token);

            var subs = await GetJsonAsync(http,
                "https://management.azure.com/subscriptions?api-version=2022-12-01", ct).ConfigureAwait(false);
            if (subs is null) return null;

            FoundryTarget? best = null;
            var bestScore = int.MinValue;

            foreach (var sub in EnumerateArray(subs, "value"))
            {
                ct.ThrowIfCancellationRequested();
                var subId = GetString(sub, "subscriptionId");
                if (string.IsNullOrEmpty(subId)) continue;

                var accountsUrl =
                    $"https://management.azure.com/subscriptions/{subId}/providers/Microsoft.CognitiveServices/accounts?api-version=2023-05-01";
                var accounts = await GetJsonAsync(http, accountsUrl, ct).ConfigureAwait(false);
                if (accounts is null) continue;

                foreach (var acct in EnumerateArray(accounts, "value"))
                {
                    var kind = GetString(acct, "kind") ?? string.Empty;
                    if (!kind.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) &&
                        !kind.Equals("AIServices", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var acctId = GetString(acct, "id");
                    var name = GetString(acct, "name") ?? "foundry";
                    var endpoint = GetEndpoint(acct);
                    if (string.IsNullOrEmpty(acctId) || string.IsNullOrEmpty(endpoint)) continue;

                    var deploymentsUrl = $"https://management.azure.com{acctId}/deployments?api-version=2023-05-01";
                    var deployments = await GetJsonAsync(http, deploymentsUrl, ct).ConfigureAwait(false);
                    if (deployments is null) continue;

                    foreach (var dep in EnumerateArray(deployments, "value"))
                    {
                        var depName = GetString(dep, "name");
                        var modelName = GetModelName(dep);
                        if (string.IsNullOrEmpty(depName) || string.IsNullOrEmpty(modelName)) continue;

                        var score = ScoreModel(modelName!);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = new FoundryTarget(name, endpoint!, depName!, modelName!);
                        }
                    }
                }
            }

            if (best is not null)
            {
                _logger?.LogInformation("Discovered Foundry deployment {Deployment} ({Model}) at {Endpoint}",
                    best.Deployment, best.ModelName, best.Endpoint);
            }

            return best;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Foundry discovery failed");
            return null;
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────────────────────

    private static int ScoreModel(string modelName)
    {
        var m = modelName.ToLowerInvariant();
        for (var i = 0; i < ChatModelPreference.Length; i++)
        {
            if (m.Contains(ChatModelPreference[i])) return ChatModelPreference.Length - i + 10;
        }
        // Any GPT / o-series chat model still beats nothing.
        return m.Contains("gpt") || m.StartsWith('o') ? 1 : int.MinValue + 1;
    }

    private static async Task<JsonElement?> GetJsonAsync(HttpClient http, string url, CancellationToken ct)
    {
        using var resp = await http.GetAsync(url, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) return null;
        var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        return doc.RootElement.Clone();
    }

    private static IEnumerable<JsonElement> EnumerateArray(JsonElement? element, string property)
    {
        if (element is { } e && e.ValueKind == JsonValueKind.Object &&
            e.TryGetProperty(property, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray()) yield return item;
        }
    }

    private static string? GetString(JsonElement e, string property)
        => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static string? GetEndpoint(JsonElement account)
    {
        if (account.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
        {
            var ep = GetString(props, "endpoint");
            if (!string.IsNullOrEmpty(ep)) return ep;
        }
        return null;
    }

    private static string? GetModelName(JsonElement deployment)
    {
        if (deployment.TryGetProperty("properties", out var props) &&
            props.TryGetProperty("model", out var model) && model.ValueKind == JsonValueKind.Object)
        {
            return GetString(model, "name");
        }
        return null;
    }

    private static Dictionary<string, string> DecodeJwtClaims(string jwt)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parts = jwt.Split('.');
        if (parts.Length < 2) return result;

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    result[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }
        }
        catch
        {
            // best effort — malformed token yields no claims
        }

        return result;
    }

    private static string? Claim(Dictionary<string, string> claims, string name)
        => claims.TryGetValue(name, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;
}
