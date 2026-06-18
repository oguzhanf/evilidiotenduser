using EvilEndUser.Core.Models;

namespace EvilEndUser.Core.Abstractions;

/// <summary>
/// The runtime context handed to a scenario (and to the LLM brain) during execution. It bundles the
/// configuration, the safety guard, the available drivers, logging, and a randomness source, and it
/// provides the single funnel — <see cref="ExecuteAsync"/> — through which all actions must pass so
/// that safety validation and telemetry are always applied.
/// </summary>
public interface IScenarioContext
{
    /// <summary>The active simulation configuration.</summary>
    SimulationConfig Config { get; }

    /// <summary>Deterministic-or-entropic randomness source honouring <see cref="SimulationConfig.RandomSeed"/>.</summary>
    Random Random { get; }

    /// <summary>The scenario run currently being recorded.</summary>
    ScenarioResult Result { get; }

    /// <summary>Emits a telemetry/log event to all sinks.</summary>
    void Log(SimulationEvent evt);

    /// <summary>Convenience helper to emit a simple informational event.</summary>
    void Log(string message, EventSeverity severity = EventSeverity.Info, string source = "Scenario");

    /// <summary>
    /// Validates an action against the <c>SafetyGuard</c>, dispatches it to the appropriate
    /// driver, records an <see cref="ActionResult"/>, and emits telemetry. This is the only supported
    /// way to perform an action; calling drivers directly bypasses safety enforcement.
    /// </summary>
    Task<ActionResult> ExecuteAsync(SimAction action, CancellationToken ct = default);

    /// <summary>Returns the driver of the requested type, or null when unavailable/disabled.</summary>
    T? GetDriver<T>() where T : class, IAutomationDriver;

    /// <summary>Sleeps for a randomised "think time" within the configured bounds.</summary>
    Task ThinkAsync(CancellationToken ct = default);
}
