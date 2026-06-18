namespace EvilEndUser.Core.Models;

/// <summary>
/// Represents how dangerous a simulated action is. Used to gate behaviour based on
/// the configured <see cref="SimulationConfig.MaxRiskLevel"/> and to colour-code the UI.
/// </summary>
public enum RiskLevel
{
    /// <summary>Benign activity that never triggers a security control (e.g. opening a blank document).</summary>
    Safe = 0,

    /// <summary>Mildly suspicious but common behaviour (e.g. visiting an unknown website).</summary>
    Low = 1,

    /// <summary>Behaviour that may trip heuristic detections (e.g. downloading an archive).</summary>
    Medium = 2,

    /// <summary>Behaviour that should reliably generate an alert (e.g. fetching the EICAR file).</summary>
    High = 3,

    /// <summary>Behaviour emulating active compromise (e.g. running an encoded PowerShell payload).</summary>
    Critical = 4,
}
