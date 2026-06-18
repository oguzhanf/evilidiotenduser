using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EvilEndUser.Automation;
using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Engine;
using EvilEndUser.Core.Events;
using EvilEndUser.Core.Models;
using EvilEndUser.LLM;
using EvilEndUser.MitreAttack;
using Microsoft.UI.Dispatching;

namespace EvilEndUser.App.Services;

/// <summary>
/// Central application-state and orchestration object the UI binds to. It owns the live configuration,
/// the scenario registry, the telemetry bus, and the run lifecycle, marshalling events onto the UI
/// thread for display.
/// </summary>
public sealed partial class SimulationService : ObservableObject
{
    private readonly DispatcherQueue _dispatcher;
    private readonly DriverFactory _driverFactory;
    private CancellationTokenSource? _cts;

    /// <summary>The live, editable configuration.</summary>
    public SimulationConfig Config { get; } = new();

    /// <summary>The telemetry bus shared by the engine and drivers.</summary>
    public ISimulationEventBus EventBus { get; } = new SimulationEventBus();

    /// <summary>All available scenarios.</summary>
    public ScenarioRegistry Scenarios { get; } = new();

    /// <summary>Observable, UI-thread-affined view of telemetry events.</summary>
    public ObservableCollection<SimulationEvent> Events { get; } = new();

    /// <summary>Completed scenario results for the current session.</summary>
    public ObservableCollection<ScenarioResult> Results { get; } = new();

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = "Idle";

    [ObservableProperty]
    private int _eventCount;

    [ObservableProperty]
    private int _riskyActionCount;

    /// <summary>Creates the service and begins listening for telemetry.</summary>
    public SimulationService()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _driverFactory = new DriverFactory();
        EventBus.EventPublished += OnEventPublished;

        // Default the self-mailbox to the signed-in Windows account where possible.
        Config.SelfMailbox = TryGetUserUpn();
    }

    private void OnEventPublished(SimulationEvent evt)
    {
        _dispatcher.TryEnqueue(() =>
        {
            Events.Insert(0, evt);
            EventCount = Events.Count;
            if (evt.RiskLevel is >= RiskLevel.Medium)
            {
                RiskyActionCount++;
            }

            // Keep the live view bounded.
            while (Events.Count > 1000)
            {
                Events.RemoveAt(Events.Count - 1);
            }
        });
    }

    /// <summary>Runs a single scenario by id.</summary>
    public Task RunScenarioAsync(string scenarioId)
    {
        var scenario = Scenarios.FindById(scenarioId);
        return scenario is null ? Task.CompletedTask : RunAsync(eng => eng.RunScenarioAsync(scenario, _cts!.Token));
    }

    /// <summary>Runs the curated full kill-chain.</summary>
    public Task RunKillChainAsync()
        => RunAsync(eng => eng.RunChainAsync(Scenarios.FullKillChain(), _cts!.Token));

    /// <summary>Runs every scenario for a tactic.</summary>
    public Task RunTacticAsync(MitreTactic tactic)
        => RunAsync(eng => eng.RunChainAsync(Scenarios.ForTactic(tactic), _cts!.Token));

    /// <summary>Runs the LLM free-roam ("wander") session.</summary>
    public Task RunWanderAsync(int steps)
        => RunAsync(eng => eng.WanderAsync(steps, _cts!.Token));

    private async Task RunAsync(Func<SimulationEngine, Task> run)
    {
        if (IsRunning) return;

        IsRunning = true;
        StatusText = "Building drivers…";
        _cts = new CancellationTokenSource();

        try
        {
            var drivers = _driverFactory.Build(Config);
            IAgentBrain? brain = Config.Llm.Provider != LlmProvider.None && Config.Llm.IsConfigured
                ? new SemanticKernelAgentBrain(Config)
                : null;

            var engine = new SimulationEngine(Config, drivers, EventBus, brain);
            StatusText = "Running…";

            var result = run(engine);
            await result;

            StatusText = "Completed";

            // Surface results.
            foreach (var r in EventBus.History
                         .Select(e => e.ScenarioId)
                         .Where(id => id is not null)
                         .Distinct()
                         .ToList())
            {
                // results are tracked by the engine's announcements; nothing extra needed here.
            }

            foreach (var driver in drivers)
            {
                await driver.DisposeAsync();
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            EventBus.Publish(new SimulationEvent
            {
                Source = "App",
                Severity = EventSeverity.Error,
                Message = $"Run failed: {ex}",
            });
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    /// <summary>Requests cancellation of the current run.</summary>
    public void Stop()
    {
        if (_cts is { IsCancellationRequested: false })
        {
            StatusText = "Stopping…";
            _cts.Cancel();
        }
    }

    /// <summary>Clears the live event view.</summary>
    public void ClearEvents()
    {
        Events.Clear();
        EventCount = 0;
        RiskyActionCount = 0;
    }

    private static string? TryGetUserUpn()
    {
        try
        {
            // UPN is often surfaced via the USERPRINCIPALNAME env var on AAD-joined machines.
            var upn = Environment.GetEnvironmentVariable("USERPRINCIPALNAME");
            if (!string.IsNullOrWhiteSpace(upn)) return upn;
        }
        catch { /* best effort */ }

        return null;
    }
}
