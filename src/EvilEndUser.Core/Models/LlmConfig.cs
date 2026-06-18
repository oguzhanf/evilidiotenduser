namespace EvilEndUser.Core.Models;

/// <summary>
/// Which LLM backend drives the agent brain.
/// </summary>
public enum LlmProvider
{
    /// <summary>Azure OpenAI (recommended; deploy a chat model in the demo tenant).</summary>
    AzureOpenAI,

    /// <summary>Public OpenAI API.</summary>
    OpenAI,

    /// <summary>Local Ollama endpoint.</summary>
    Ollama,

    /// <summary>No LLM — fall back to deterministic scenario scripting only.</summary>
    None,
}

/// <summary>
/// Connection + behaviour settings for the LLM brain. Secrets are intentionally <em>not</em>
/// persisted to disk by the app; they are read from the Windows Credential Manager / environment.
/// </summary>
public sealed class LlmConfig
{
    /// <summary>The backend provider to use.</summary>
    public LlmProvider Provider { get; set; } = LlmProvider.AzureOpenAI;

    /// <summary>Azure OpenAI resource endpoint, e.g. <c>https://my-aoai.openai.azure.com/</c>.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Deployment / model name, e.g. <c>gpt-4o</c>.</summary>
    public string DeploymentName { get; set; } = "gpt-4o";

    /// <summary>
    /// API key. When null, the app uses <c>DefaultAzureCredential</c> / interactive sign-in instead.
    /// Never serialized by the configuration store.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Entra tenant ID used for interactive token auth when no API key is supplied.</summary>
    public string? TenantId { get; set; }

    /// <summary>Sampling temperature applied on top of the persona's erraticness.</summary>
    public double BaseTemperature { get; set; } = 0.7;

    /// <summary>Maximum tokens per brain decision.</summary>
    public int MaxTokens { get; set; } = 800;

    /// <summary>True when enough configuration is present to attempt a connection.</summary>
    public bool IsConfigured => Provider switch
    {
        LlmProvider.AzureOpenAI => !string.IsNullOrWhiteSpace(Endpoint),
        LlmProvider.OpenAI => !string.IsNullOrWhiteSpace(ApiKey),
        LlmProvider.Ollama => !string.IsNullOrWhiteSpace(Endpoint),
        _ => false,
    };
}
