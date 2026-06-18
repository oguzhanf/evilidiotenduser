namespace EvilEndUser.Core.Models;

/// <summary>
/// The MITRE ATT&amp;CK Enterprise tactics (kill-chain stages) covered by this simulator.
/// IDs and names reflect Enterprise ATT&amp;CK v19.1 (May 2026), including the v17 split of the
/// legacy "Defense Evasion" tactic into <see cref="Stealth"/> and <see cref="DefenseImpairment"/>.
/// </summary>
public enum MitreTactic
{
    /// <summary>TA0043 — Gathering information to plan future operations.</summary>
    Reconnaissance,

    /// <summary>TA0042 — Establishing resources/infrastructure for operations.</summary>
    ResourceDevelopment,

    /// <summary>TA0001 — Getting the first foothold on the endpoint.</summary>
    InitialAccess,

    /// <summary>TA0002 — Running adversary-controlled code.</summary>
    Execution,

    /// <summary>TA0003 — Maintaining a foothold across reboots/logons.</summary>
    Persistence,

    /// <summary>TA0004 — Gaining higher-level permissions.</summary>
    PrivilegeEscalation,

    /// <summary>TA0005 — Hiding actions to appear as normal behaviour (renamed from "Defense Evasion" in v17).</summary>
    Stealth,

    /// <summary>TA0112 — Breaking security tooling so defenders cannot see activity (new in v17).</summary>
    DefenseImpairment,

    /// <summary>TA0006 — Stealing account names and passwords.</summary>
    CredentialAccess,

    /// <summary>TA0007 — Enumerating the environment.</summary>
    Discovery,

    /// <summary>TA0008 — Moving through the environment.</summary>
    LateralMovement,

    /// <summary>TA0009 — Gathering data of interest.</summary>
    Collection,

    /// <summary>TA0011 — Communicating with compromised systems.</summary>
    CommandAndControl,

    /// <summary>TA0010 — Stealing data.</summary>
    Exfiltration,

    /// <summary>TA0040 — Manipulating, interrupting, or destroying systems/data.</summary>
    Impact,
}

/// <summary>
/// Metadata describing a single MITRE ATT&amp;CK tactic, including its canonical ID and URL.
/// </summary>
public sealed record MitreTacticInfo(MitreTactic Tactic, string Id, string Name, string Description)
{
    /// <summary>The canonical attack.mitre.org URL for this tactic.</summary>
    public string Url => $"https://attack.mitre.org/tactics/{Id}/";
}

/// <summary>
/// Static catalogue of MITRE ATT&amp;CK tactic metadata (Enterprise v19.1).
/// </summary>
public static class MitreTactics
{
    /// <summary>All tactics in kill-chain order with their canonical IDs/descriptions.</summary>
    public static readonly IReadOnlyList<MitreTacticInfo> All = new[]
    {
        new MitreTacticInfo(MitreTactic.Reconnaissance, "TA0043", "Reconnaissance", "Gathering information to plan future operations."),
        new MitreTacticInfo(MitreTactic.ResourceDevelopment, "TA0042", "Resource Development", "Establishing resources to support operations."),
        new MitreTacticInfo(MitreTactic.InitialAccess, "TA0001", "Initial Access", "Getting into the target system."),
        new MitreTacticInfo(MitreTactic.Execution, "TA0002", "Execution", "Running malicious code."),
        new MitreTacticInfo(MitreTactic.Persistence, "TA0003", "Persistence", "Maintaining a foothold across reboots and logons."),
        new MitreTacticInfo(MitreTactic.PrivilegeEscalation, "TA0004", "Privilege Escalation", "Gaining higher-level permissions."),
        new MitreTacticInfo(MitreTactic.Stealth, "TA0005", "Stealth", "Hiding actions to appear as normal behaviour."),
        new MitreTacticInfo(MitreTactic.DefenseImpairment, "TA0112", "Defense Impairment", "Breaking security tools so defenders cannot see activity."),
        new MitreTacticInfo(MitreTactic.CredentialAccess, "TA0006", "Credential Access", "Stealing account names and passwords."),
        new MitreTacticInfo(MitreTactic.Discovery, "TA0007", "Discovery", "Figuring out the environment."),
        new MitreTacticInfo(MitreTactic.LateralMovement, "TA0008", "Lateral Movement", "Moving through the environment."),
        new MitreTacticInfo(MitreTactic.Collection, "TA0009", "Collection", "Gathering data of interest."),
        new MitreTacticInfo(MitreTactic.CommandAndControl, "TA0011", "Command and Control", "Communicating with compromised systems."),
        new MitreTacticInfo(MitreTactic.Exfiltration, "TA0010", "Exfiltration", "Stealing data."),
        new MitreTacticInfo(MitreTactic.Impact, "TA0040", "Impact", "Manipulating, interrupting, or destroying systems/data."),
    };

    /// <summary>Looks up metadata for a tactic enum value.</summary>
    public static MitreTacticInfo Get(MitreTactic tactic) => All.First(t => t.Tactic == tactic);
}
