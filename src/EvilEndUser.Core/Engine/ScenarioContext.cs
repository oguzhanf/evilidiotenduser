using System.Diagnostics;
using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Events;
using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;

namespace EvilEndUser.Core.Engine;

/// <summary>
/// Default <see cref="IScenarioContext"/> implementation. It owns the single action-execution funnel
/// that applies safety validation, dispatches to the correct driver, records results, and publishes
/// telemetry. Scenarios and the LLM brain interact with the world exclusively through this object.
/// </summary>
public sealed class ScenarioContext : IScenarioContext
{
    private readonly IReadOnlyList<IAutomationDriver> _drivers;
    private readonly ISimulationEventBus _bus;
    private readonly SafetyGuard _guard;
    private readonly string? _scenarioId;

    /// <summary>Creates a context for a single scenario run.</summary>
    public ScenarioContext(
        SimulationConfig config,
        ScenarioResult result,
        IReadOnlyList<IAutomationDriver> drivers,
        ISimulationEventBus bus,
        Random random,
        string? scenarioId = null)
    {
        Config = config;
        Result = result;
        _drivers = drivers;
        _bus = bus;
        Random = random;
        _guard = new SafetyGuard(config);
        _scenarioId = scenarioId;
    }

    /// <inheritdoc />
    public SimulationConfig Config { get; }

    /// <inheritdoc />
    public Random Random { get; }

    /// <inheritdoc />
    public ScenarioResult Result { get; }

    /// <inheritdoc />
    public void Log(SimulationEvent evt) => _bus.Publish(evt);

    /// <inheritdoc />
    public void Log(string message, EventSeverity severity = EventSeverity.Info, string source = "Scenario")
        => _bus.Publish(new SimulationEvent
        {
            Message = message,
            Severity = severity,
            Source = source,
            ScenarioId = _scenarioId,
        });

    /// <inheritdoc />
    public async Task<ActionResult> ExecuteAsync(SimAction action, CancellationToken ct = default)
    {
        // 1) Safety: never execute anything the guard rejects.
        try
        {
            _guard.Validate(action);
        }
        catch (SafetyViolationException ex)
        {
            var blocked = ActionResult.Fail(action, "Blocked by safety guard", ex.Message);
            Result.Actions.Add(blocked);
            PublishActionEvent(action, blocked, EventSeverity.Warning, "blocked");
            return blocked;
        }

        // 2) Dry-run short-circuit.
        if (Config.DryRun)
        {
            var dry = ActionResult.Ok(action, "[dry-run] action not executed");
            Result.Actions.Add(dry);
            PublishActionEvent(action, dry, EventSeverity.Info, "dry-run");
            return dry;
        }

        // 3) Dispatch to a driver.
        var sw = Stopwatch.StartNew();
        ActionResult result;
        try
        {
            result = await DispatchAsync(action, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result = ActionResult.Fail(action, "Driver threw", ex.ToString(), sw.Elapsed);
        }
        sw.Stop();

        Result.Actions.Add(result);
        PublishActionEvent(action, result,
            result.Success ? EventSeverity.Telemetry : EventSeverity.Error,
            result.Success ? "executed" : "failed");
        return result;
    }

    private async Task<ActionResult> DispatchAsync(SimAction action, CancellationToken ct)
    {
        switch (action.Type)
        {
            case SimActionType.BrowseUrl:
                return await Require<IBrowserDriver>(action).NavigateAsync(action, ct);
            case SimActionType.DownloadFile:
                return await Require<IBrowserDriver>(action).DownloadAsync(action, ct);
            case SimActionType.SubmitWebForm:
                return await Require<IBrowserDriver>(action).SubmitFormAsync(action, ct);

            case SimActionType.SendEmail:
                return await Require<IEmailDriver>(action).SendAsync(action, ct);
            case SimActionType.OpenAttachment:
                return await Require<IEmailDriver>(action).OpenAttachmentAsync(action, ct);
            case SimActionType.ClickEmailLink:
                // Clicking a link in an email is functionally a browser navigation.
                return await Require<IBrowserDriver>(action).NavigateAsync(action, ct);

            case SimActionType.SendTeamsMessage:
                return await Require<ITeamsDriver>(action).SendMessageAsync(action, ct);

            case SimActionType.OpenFile:
                return await Require<IFileSystemDriver>(action).OpenFileAsync(action, ct);
            case SimActionType.WriteFile:
                return await Require<IFileSystemDriver>(action).WriteFileAsync(action, ct);
            case SimActionType.UploadToCloud:
                return await Require<IBrowserDriver>(action).NavigateAsync(action, ct);

            case SimActionType.LaunchDesktopApp:
                return await Require<IDesktopDriver>(action).LaunchAppAsync(action, ct);

            case SimActionType.RunProcess:
                return await Require<IProcessDriver>(action).RunAsync(action, ct);
            case SimActionType.ModifyRegistry:
                return await Require<IProcessDriver>(action).ModifyRegistryAsync(action, ct);

            case SimActionType.Idle:
                await ThinkAsync(ct);
                return ActionResult.Ok(action, "Idled");

            default:
                return ActionResult.Fail(action, $"No dispatcher for action type {action.Type}");
        }
    }

    private TDriver Require<TDriver>(SimAction action) where TDriver : class, IAutomationDriver
    {
        var driver = GetDriver<TDriver>();
        if (driver is null)
        {
            throw new InvalidOperationException(
                $"Action {action.Type} requires driver {typeof(TDriver).Name}, which is not available/enabled.");
        }

        return driver;
    }

    /// <inheritdoc />
    public T? GetDriver<T>() where T : class, IAutomationDriver
        => _drivers.OfType<T>().FirstOrDefault();

    /// <inheritdoc />
    public async Task ThinkAsync(CancellationToken ct = default)
    {
        var min = Config.MinThinkTime;
        var max = Config.MaxThinkTime;
        if (max < min) max = min;
        var span = max - min;
        var delay = min + TimeSpan.FromMilliseconds(Random.NextDouble() * span.TotalMilliseconds);
        await Task.Delay(delay, ct).ConfigureAwait(false);
    }

    private void PublishActionEvent(SimAction action, ActionResult result, EventSeverity severity, string verb)
        => _bus.Publish(new SimulationEvent
        {
            Severity = severity,
            Source = action.Type.ToString(),
            Message = $"{verb}: {action.Type} {action.Target} — {result.Message}",
            TechniqueId = action.Technique?.Id,
            Tactic = action.Technique?.Tactic,
            RiskLevel = action.RiskLevel,
            ScenarioId = _scenarioId,
            Data = BuildData(action, result),
        });

    private static Dictionary<string, string> BuildData(SimAction action, ActionResult result)
    {
        var data = new Dictionary<string, string>
        {
            ["actionId"] = action.Id.ToString(),
            ["success"] = result.Success.ToString(),
        };
        if (action.Technique is not null) data["techniqueName"] = action.Technique.Name;
        if (!string.IsNullOrEmpty(action.Rationale)) data["rationale"] = action.Rationale!;
        if (result.Artifacts.Count > 0) data["artifacts"] = string.Join(";", result.Artifacts);
        return data;
    }
}
