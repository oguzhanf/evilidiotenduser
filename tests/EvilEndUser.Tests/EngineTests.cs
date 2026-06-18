using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Engine;
using EvilEndUser.Core.Events;
using EvilEndUser.Core.Models;
using EvilEndUser.MitreAttack.Scenarios.InitialAccess;
using Xunit;

namespace EvilEndUser.Tests;

public class EngineTests
{
    private static SimulationConfig FastConfig(RiskLevel ceiling = RiskLevel.High) => new()
    {
        MaxRiskLevel = ceiling,
        SafeMode = true,
        MinThinkTime = TimeSpan.Zero,
        MaxThinkTime = TimeSpan.Zero,
        RandomSeed = 1234,
    };

    [Fact]
    public async Task DriveBy_scenario_navigates_to_safe_urls()
    {
        var browser = new FakeBrowserDriver();
        var bus = new SimulationEventBus();
        var engine = new SimulationEngine(FastConfig(), new IAutomationDriver[] { browser }, bus);

        var result = await engine.RunScenarioAsync(new DriveByCompromiseScenario());

        Assert.Equal(ScenarioStatus.Completed, result.Status);
        Assert.NotEmpty(browser.Navigations);
        Assert.All(browser.Navigations, a => Assert.False(string.IsNullOrWhiteSpace(a.Target)));
        Assert.Contains(result.TechniquesExercised, id => id == "T1189");
    }

    [Fact]
    public async Task Scenario_is_skipped_when_above_ceiling()
    {
        var browser = new FakeBrowserDriver();
        var bus = new SimulationEventBus();
        // Spearphishing scenario is High risk; ceiling Low must skip it.
        var engine = new SimulationEngine(FastConfig(RiskLevel.Low), new IAutomationDriver[] { browser }, bus);

        var result = await engine.RunScenarioAsync(new SpearphishingLinkScenario());

        Assert.Equal(ScenarioStatus.Skipped, result.Status);
        Assert.Empty(browser.Navigations);
    }

    [Fact]
    public async Task DryRun_records_actions_without_calling_driver()
    {
        var browser = new FakeBrowserDriver();
        var bus = new SimulationEventBus();
        var config = FastConfig();
        config.DryRun = true;
        var engine = new SimulationEngine(config, new IAutomationDriver[] { browser }, bus);

        var result = await engine.RunScenarioAsync(new DriveByCompromiseScenario());

        Assert.Equal(ScenarioStatus.Completed, result.Status);
        Assert.NotEmpty(result.Actions);
        Assert.Empty(browser.Navigations); // driver never invoked in dry-run
    }

    [Fact]
    public async Task Events_are_published_to_the_bus()
    {
        var browser = new FakeBrowserDriver();
        var bus = new SimulationEventBus();
        var engine = new SimulationEngine(FastConfig(), new IAutomationDriver[] { browser }, bus);

        await engine.RunScenarioAsync(new DriveByCompromiseScenario());

        Assert.NotEmpty(bus.History);
        Assert.Contains(bus.History, e => e.Severity == EventSeverity.Telemetry);
    }
}
