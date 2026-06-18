using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace EvilEndUser.Automation.Graph;

/// <summary>
/// Lazily builds and caches an authenticated <see cref="GraphServiceClient"/> for the signed-in
/// simulated user. Uses interactive or device-code sign-in (suitable for a desktop app and for the
/// "device login" flow the operator uses to grant access to the demo tenant).
/// </summary>
public sealed class GraphClientProvider : IAsyncDisposable
{
    /// <summary>Delegated scopes required by the email and Teams drivers.</summary>
    public static readonly string[] Scopes =
    {
        "User.Read",
        "Mail.Send",
        "Mail.ReadWrite",
        "Chat.ReadWrite",
        "ChatMessage.Send",
        "Files.ReadWrite",
    };

    private readonly string? _tenantId;
    private readonly ILogger? _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly bool _useDeviceCode;

    private GraphServiceClient? _client;

    /// <summary>Creates a provider.</summary>
    /// <param name="tenantId">Optional Entra tenant ID (defaults to "organizations").</param>
    /// <param name="useDeviceCode">When true, uses device-code flow (prints a code to enter at aka.ms/devicelogin).</param>
    /// <param name="logger">Optional logger.</param>
    public GraphClientProvider(string? tenantId = null, bool useDeviceCode = true, ILogger? logger = null)
    {
        _tenantId = tenantId;
        _useDeviceCode = useDeviceCode;
        _logger = logger;
    }

    /// <summary>
    /// Optional callback invoked with the device-code prompt so the UI can surface the code/URL.
    /// </summary>
    public Func<DeviceCodeInfo, CancellationToken, Task>? DeviceCodeCallback { get; set; }

    /// <summary>Returns the authenticated client, signing in on first use.</summary>
    public async Task<GraphServiceClient> GetClientAsync(CancellationToken ct = default)
    {
        if (_client is not null) return _client;

        await _lock.WaitAsync(ct);
        try
        {
            if (_client is not null) return _client;

            TokenCredential credential = _useDeviceCode
                ? new DeviceCodeCredential(new DeviceCodeCredentialOptions
                {
                    TenantId = _tenantId ?? "organizations",
                    ClientId = WellKnownClientIds.AzureCli,
                    DeviceCodeCallback = (info, token) =>
                    {
                        _logger?.LogInformation("Device code: {Message}", info.Message);
                        return DeviceCodeCallback is not null
                            ? DeviceCodeCallback(info, token)
                            : Console.Out.WriteLineAsync(info.Message);
                    },
                })
                : new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = _tenantId ?? "organizations",
                    ClientId = WellKnownClientIds.AzureCli,
                });

            _client = new GraphServiceClient(credential, Scopes);
            _logger?.LogInformation("Graph client created (tenant {Tenant})", _tenantId ?? "organizations");
            return _client;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _lock.Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>Well-known public client IDs usable for delegated desktop sign-in.</summary>
public static class WellKnownClientIds
{
    /// <summary>The Azure CLI first-party client ID — a public client that supports device-code flow.</summary>
    public const string AzureCli = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";
}
