namespace EvilEndUser.Core.Models;

/// <summary>
/// Top-level configuration for a simulation run. Controls which apps are driven, the safety
/// ceiling, the target addresses, the persona, and the LLM connection.
/// </summary>
public sealed class SimulationConfig
{
    /// <summary>
    /// Hard safety ceiling. No action above this risk level is ever executed, regardless of what
    /// a scenario or the LLM requests. Defaults to <see cref="RiskLevel.High"/> so that genuinely
    /// destructive emulation (<see cref="RiskLevel.Critical"/>) must be explicitly opted into.
    /// </summary>
    public RiskLevel MaxRiskLevel { get; set; } = RiskLevel.High;

    /// <summary>
    /// Master safety switch. When true (default) the simulator only ever uses the vetted, harmless
    /// test payloads in <see cref="Safety.TestPayloads"/> and refuses real malware / arbitrary URLs.
    /// </summary>
    public bool SafeMode { get; set; } = true;

    /// <summary>When true, actions are logged but not actually executed (useful for dry runs).</summary>
    public bool DryRun { get; set; }

    /// <summary>The persona the simulated user adopts.</summary>
    public PersonaProfile Persona { get; set; } = new();

    /// <summary>LLM brain configuration.</summary>
    public LlmConfig Llm { get; set; } = new();

    /// <summary>Which browser to drive for web-based techniques.</summary>
    public BrowserKind Browser { get; set; } = BrowserKind.Edge;

    /// <summary>Run the browser headless. Off by default so the operator can watch the simulation.</summary>
    public bool HeadlessBrowser { get; set; }

    /// <summary>Enable driving the Outlook desktop / Graph mail capability.</summary>
    public bool EnableEmail { get; set; } = true;

    /// <summary>Enable driving Microsoft Teams.</summary>
    public bool EnableTeams { get; set; } = true;

    /// <summary>Enable running shell processes (PowerShell/cmd) for execution techniques.</summary>
    public bool EnableProcessExecution { get; set; } = true;

    /// <summary>Enable filesystem drops (e.g. writing the EICAR test file to Downloads).</summary>
    public bool EnableFileSystem { get; set; } = true;

    /// <summary>
    /// The mailbox the simulated user "owns" (the signed-in identity on this endpoint). Used as the
    /// sender for outbound mail and the inbox that is read for phishing links.
    /// </summary>
    public string? SelfMailbox { get; set; }

    /// <summary>
    /// External destination(s) used to emulate self-exfiltration / data sharing (e.g. a personal
    /// Gmail address controlled by the red team). Multiple addresses may be supplied.
    /// </summary>
    public List<string> ExternalRecipients { get; set; } = new();

    /// <summary>Minimum think-time between actions.</summary>
    public TimeSpan MinThinkTime { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Maximum think-time between actions (a random value in [min,max] is used).</summary>
    public TimeSpan MaxThinkTime { get; set; } = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Optional fixed RNG seed for reproducible "random" choices. Null = system entropy.
    /// </summary>
    public int? RandomSeed { get; set; }

    /// <summary>Folder where dropped test artefacts and exported logs are written.</summary>
    public string WorkingDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    /// <summary>Returns true if the given risk level is permitted by the current ceiling.</summary>
    public bool Permits(RiskLevel level) => level <= MaxRiskLevel;
}
