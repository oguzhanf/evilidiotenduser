namespace EvilEndUser.Core.Models;

/// <summary>
/// Severity used when emitting <see cref="SimulationEvent"/>s to the UI / log sinks.
/// </summary>
public enum EventSeverity
{
    /// <summary>Verbose diagnostic detail.</summary>
    Debug,

    /// <summary>Normal informational progress.</summary>
    Info,

    /// <summary>A simulated risky action was taken (expected to generate a Defender alert).</summary>
    Telemetry,

    /// <summary>Something unexpected but non-fatal.</summary>
    Warning,

    /// <summary>An action or scenario failed.</summary>
    Error,
}

/// <summary>
/// An immutable telemetry record describing something the simulator did. These are streamed to
/// the WinUI activity log and can be exported for correlation against Defender alerts.
/// </summary>
public sealed class SimulationEvent
{
    /// <summary>Unique identifier for the event.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>When the event occurred.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    /// <summary>Severity / classification.</summary>
    public EventSeverity Severity { get; init; } = EventSeverity.Info;

    /// <summary>Short source component name (e.g. "BrowserDriver", "AgentBrain").</summary>
    public string Source { get; init; } = "Simulator";

    /// <summary>Human-readable message.</summary>
    public required string Message { get; init; }

    /// <summary>The MITRE technique associated with the event, if any (e.g. "T1566.002").</summary>
    public string? TechniqueId { get; init; }

    /// <summary>The owning tactic, if any.</summary>
    public MitreTactic? Tactic { get; init; }

    /// <summary>The risk level of the underlying action, if any.</summary>
    public RiskLevel? RiskLevel { get; init; }

    /// <summary>The scenario run this event belongs to, if any.</summary>
    public string? ScenarioId { get; init; }

    /// <summary>Structured key/value detail for export.</summary>
    public IReadOnlyDictionary<string, string> Data { get; init; }
        = new Dictionary<string, string>();

    /// <summary>The event time formatted as local HH:mm:ss for display.</summary>
    public string LocalTimeText => Timestamp.LocalDateTime.ToString("HH:mm:ss");
}
