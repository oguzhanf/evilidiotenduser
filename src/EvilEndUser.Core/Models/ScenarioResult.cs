namespace EvilEndUser.Core.Models;

/// <summary>
/// Lifecycle states of a scenario run.
/// </summary>
public enum ScenarioStatus
{
    /// <summary>Defined but not yet started.</summary>
    Pending,

    /// <summary>Currently executing.</summary>
    Running,

    /// <summary>Finished and every action succeeded.</summary>
    Completed,

    /// <summary>Finished but one or more actions failed.</summary>
    CompletedWithErrors,

    /// <summary>Stopped early because of a fatal error.</summary>
    Faulted,

    /// <summary>Cancelled by the operator.</summary>
    Cancelled,

    /// <summary>Skipped because it exceeded the configured risk ceiling or a prerequisite was missing.</summary>
    Skipped,
}

/// <summary>
/// Aggregated result of running an entire scenario (one or more <see cref="SimAction"/>s
/// emulating a MITRE technique chain).
/// </summary>
public sealed class ScenarioResult
{
    /// <summary>Identifier of the scenario that produced this result.</summary>
    public required string ScenarioId { get; init; }

    /// <summary>Display name of the scenario.</summary>
    public required string ScenarioName { get; init; }

    /// <summary>The primary tactic the scenario exercises.</summary>
    public MitreTactic Tactic { get; init; }

    /// <summary>Final status of the run.</summary>
    public ScenarioStatus Status { get; set; } = ScenarioStatus.Pending;

    /// <summary>When the run started.</summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>When the run finished (null while running).</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Per-action results captured during the run.</summary>
    public List<ActionResult> Actions { get; } = new();

    /// <summary>Summary message describing the outcome.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Distinct MITRE technique IDs that were exercised.</summary>
    public IReadOnlyList<string> TechniquesExercised =>
        Actions.Where(a => a.Action.Technique is not null)
               .Select(a => a.Action.Technique!.Id)
               .Distinct()
               .ToList();

    /// <summary>Total wall-clock duration of the run.</summary>
    public TimeSpan Duration => (CompletedAt ?? DateTimeOffset.Now) - StartedAt;

    /// <summary>Number of actions that succeeded.</summary>
    public int SucceededCount => Actions.Count(a => a.Success);

    /// <summary>Number of actions that failed.</summary>
    public int FailedCount => Actions.Count(a => !a.Success);
}
