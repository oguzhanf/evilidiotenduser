using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Events;
using EvilEndUser.Core.Models;

namespace EvilEndUser.Core.Engine;

/// <summary>
/// Orchestrates simulation runs. It wires together the configuration, the available drivers, the
/// optional LLM brain, and the event bus, and exposes high-level operations to run a single
/// scenario, a chain of scenarios, or a free-form LLM-driven "wander" session.
/// </summary>
public sealed class SimulationEngine
{
    private readonly SimulationConfig _config;
    private readonly IReadOnlyList<IAutomationDriver> _drivers;
    private readonly ISimulationEventBus _bus;
    private readonly IAgentBrain? _brain;

    /// <summary>Creates an engine.</summary>
    /// <param name="config">The active configuration.</param>
    /// <param name="drivers">The set of automation drivers enabled for this run.</param>
    /// <param name="bus">The telemetry bus.</param>
    /// <param name="brain">Optional LLM brain; when null the engine runs deterministic scenarios only.</param>
    public SimulationEngine(
        SimulationConfig config,
        IReadOnlyList<IAutomationDriver> drivers,
        ISimulationEventBus bus,
        IAgentBrain? brain = null)
    {
        _config = config;
        _drivers = drivers;
        _bus = bus;
        _brain = brain;
    }

    private Random CreateRandom()
        => _config.RandomSeed is { } seed ? new Random(seed) : new Random();

    /// <summary>Runs a single scenario end-to-end and returns its result.</summary>
    public async Task<ScenarioResult> RunScenarioAsync(IScenario scenario, CancellationToken ct = default)
    {
        var result = new ScenarioResult
        {
            ScenarioId = scenario.Id,
            ScenarioName = scenario.Name,
            Tactic = scenario.Tactic,
            Status = ScenarioStatus.Running,
        };

        var context = new ScenarioContext(_config, result, _drivers, _bus, CreateRandom(), scenario.Id);

        _bus.Publish(new SimulationEvent
        {
            Source = "Engine",
            Severity = EventSeverity.Info,
            Message = $"▶ Starting scenario '{scenario.Name}' ({scenario.Tactic})",
            ScenarioId = scenario.Id,
            Tactic = scenario.Tactic,
        });

        // Respect the global risk ceiling before doing anything.
        if (!_config.Permits(scenario.MaxRiskLevel))
        {
            result.Status = ScenarioStatus.Skipped;
            result.Summary = $"Skipped: scenario risk {scenario.MaxRiskLevel} exceeds ceiling {_config.MaxRiskLevel}.";
            result.CompletedAt = DateTimeOffset.Now;
            Announce(result);
            return result;
        }

        if (!scenario.CanRun(context))
        {
            result.Status = ScenarioStatus.Skipped;
            result.Summary = "Skipped: prerequisites/drivers not available.";
            result.CompletedAt = DateTimeOffset.Now;
            Announce(result);
            return result;
        }

        try
        {
            await scenario.RunAsync(context, ct).ConfigureAwait(false);
            result.Status = result.FailedCount == 0
                ? ScenarioStatus.Completed
                : ScenarioStatus.CompletedWithErrors;
            result.Summary = $"{result.SucceededCount} succeeded, {result.FailedCount} failed; " +
                             $"techniques: {string.Join(", ", result.TechniquesExercised)}.";
        }
        catch (OperationCanceledException)
        {
            result.Status = ScenarioStatus.Cancelled;
            result.Summary = "Cancelled by operator.";
        }
        catch (Exception ex)
        {
            result.Status = ScenarioStatus.Faulted;
            result.Summary = $"Faulted: {ex.Message}";
            _bus.Publish(new SimulationEvent
            {
                Source = "Engine",
                Severity = EventSeverity.Error,
                Message = $"Scenario '{scenario.Name}' faulted: {ex}",
                ScenarioId = scenario.Id,
            });
        }
        finally
        {
            result.CompletedAt = DateTimeOffset.Now;
        }

        Announce(result);
        return result;
    }

    /// <summary>Runs an ordered chain of scenarios (e.g. a full kill-chain), stopping on cancellation.</summary>
    public async Task<IReadOnlyList<ScenarioResult>> RunChainAsync(
        IEnumerable<IScenario> scenarios,
        CancellationToken ct = default)
    {
        var results = new List<ScenarioResult>();
        foreach (var scenario in scenarios)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(await RunScenarioAsync(scenario, ct).ConfigureAwait(false));
        }

        return results;
    }

    /// <summary>
    /// Runs a free-form LLM-driven session in which the brain improvises actions for up to
    /// <paramref name="maxSteps"/> steps. Falls back to a no-op when no brain is configured.
    /// </summary>
    public async Task<ScenarioResult> WanderAsync(int maxSteps, CancellationToken ct = default)
    {
        var result = new ScenarioResult
        {
            ScenarioId = "llm.wander",
            ScenarioName = "LLM Free Roam",
            Tactic = MitreTactic.Execution,
            Status = ScenarioStatus.Running,
        };
        var context = new ScenarioContext(_config, result, _drivers, _bus, CreateRandom(), result.ScenarioId);

        if (_brain is null || !_brain.IsEnabled)
        {
            result.Status = ScenarioStatus.Skipped;
            result.Summary = "Skipped: no LLM brain configured.";
            result.CompletedAt = DateTimeOffset.Now;
            Announce(result);
            return result;
        }

        try
        {
            await _brain.WanderAsync(context, maxSteps, ct).ConfigureAwait(false);
            result.Status = ScenarioStatus.Completed;
            result.Summary = $"LLM performed {result.Actions.Count} actions.";
        }
        catch (OperationCanceledException)
        {
            result.Status = ScenarioStatus.Cancelled;
            result.Summary = "Cancelled by operator.";
        }
        catch (Exception ex)
        {
            result.Status = ScenarioStatus.Faulted;
            result.Summary = $"Faulted: {ex.Message}";
        }
        finally
        {
            result.CompletedAt = DateTimeOffset.Now;
        }

        Announce(result);
        return result;
    }

    private void Announce(ScenarioResult result)
        => _bus.Publish(new SimulationEvent
        {
            Source = "Engine",
            Severity = result.Status is ScenarioStatus.Faulted ? EventSeverity.Error : EventSeverity.Info,
            Message = $"■ Scenario '{result.ScenarioName}' finished: {result.Status}. {result.Summary}",
            ScenarioId = result.ScenarioId,
            Tactic = result.Tactic,
        });
}
