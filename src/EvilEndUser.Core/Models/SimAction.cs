namespace EvilEndUser.Core.Models;

/// <summary>
/// A discrete, atomic action that the simulated user performs on the endpoint. Actions are
/// produced either by a deterministic scenario or by the LLM "brain", then executed by an
/// <see cref="Abstractions.IAutomationDriver"/> and recorded as <see cref="SimulationEvent"/>s.
/// </summary>
public sealed class SimAction
{
    /// <summary>Stable identifier for this action instance.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The category of action (drives which driver handles it).</summary>
    public required SimActionType Type { get; init; }

    /// <summary>Primary target of the action: a URL, file path, recipient, or command.</summary>
    public string? Target { get; init; }

    /// <summary>Free-form additional parameters keyed by name (e.g. "subject", "body").</summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; }
        = new Dictionary<string, string>();

    /// <summary>How risky this action is; checked against the configured maximum before execution.</summary>
    public RiskLevel RiskLevel { get; init; } = RiskLevel.Safe;

    /// <summary>The MITRE technique this action maps to, if any.</summary>
    public MitreTechnique? Technique { get; init; }

    /// <summary>Human-readable narration of why the "user" is taking this action.</summary>
    public string? Rationale { get; init; }

    /// <summary>Returns a parameter value or <paramref name="fallback"/> when absent.</summary>
    public string Param(string key, string fallback = "")
        => Parameters.TryGetValue(key, out var v) ? v : fallback;

    /// <inheritdoc />
    public override string ToString()
        => $"{Type}({Target}) [{RiskLevel}]{(Technique is null ? "" : $" {Technique.Id}")}";
}

/// <summary>
/// The categories of action a simulated user can take. Each maps to a capability exposed by
/// one of the automation drivers.
/// </summary>
public enum SimActionType
{
    /// <summary>Open a URL in a real browser (Edge/Chrome).</summary>
    BrowseUrl,

    /// <summary>Download a file from a URL via the browser.</summary>
    DownloadFile,

    /// <summary>Type credentials into a web login form.</summary>
    SubmitWebForm,

    /// <summary>Click a link inside a received email.</summary>
    ClickEmailLink,

    /// <summary>Open / "double-click" an email attachment.</summary>
    OpenAttachment,

    /// <summary>Compose and send an email (optionally to an external address).</summary>
    SendEmail,

    /// <summary>Send a Teams chat message, optionally with a link or file.</summary>
    SendTeamsMessage,

    /// <summary>Open or double-click a file on the local filesystem.</summary>
    OpenFile,

    /// <summary>Write a file to disk (e.g. drop a test payload into Downloads).</summary>
    WriteFile,

    /// <summary>Run a process / shell command (PowerShell, cmd).</summary>
    RunProcess,

    /// <summary>Modify the registry under the current user hive.</summary>
    ModifyRegistry,

    /// <summary>Upload a file to an external web service (exfiltration emulation).</summary>
    UploadToCloud,

    /// <summary>Wait / idle for a period (think time).</summary>
    Idle,
}
