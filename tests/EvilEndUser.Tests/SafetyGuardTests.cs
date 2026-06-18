using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;
using Xunit;

namespace EvilEndUser.Tests;

public class SafetyGuardTests
{
    private static SimAction Browse(string url, RiskLevel risk = RiskLevel.Medium) => new()
    {
        Type = SimActionType.BrowseUrl,
        Target = url,
        RiskLevel = risk,
    };

    private static SimAction Run(string cmd, RiskLevel risk = RiskLevel.High) => new()
    {
        Type = SimActionType.RunProcess,
        Target = cmd,
        RiskLevel = risk,
    };

    [Fact]
    public void Blocks_action_above_risk_ceiling()
    {
        var guard = new SafetyGuard(new SimulationConfig { MaxRiskLevel = RiskLevel.Medium });
        var action = Browse(TestPayloads.SmartScreenDemos[0].Url, RiskLevel.High);

        Assert.Throws<SafetyViolationException>(() => guard.Validate(action));
    }

    [Fact]
    public void Allows_action_at_or_below_ceiling()
    {
        var guard = new SafetyGuard(new SimulationConfig { MaxRiskLevel = RiskLevel.High, SafeMode = true });
        var action = Browse(TestPayloads.SmartScreenDemos[0].Url, RiskLevel.High);

        var result = guard.Validate(action);
        Assert.Same(action, result);
    }

    [Fact]
    public void SafeMode_blocks_unlisted_url()
    {
        var guard = new SafetyGuard(new SimulationConfig { SafeMode = true, MaxRiskLevel = RiskLevel.Critical });
        var action = Browse("https://evil.example.com/payload", RiskLevel.Low);

        Assert.Throws<SafetyViolationException>(() => guard.Validate(action));
    }

    [Theory]
    [InlineData("https://secure.eicar.org/eicar.com.txt")]
    [InlineData("https://demo.smartscreen.msft.net/phishingdemo.html")]
    [InlineData("https://smartscreentestratings2.net/")]
    [InlineData("https://www.amtso.org/feature-settings-check-cloud-lookup/")]
    public void SafeMode_allows_vetted_hosts(string url)
    {
        var guard = new SafetyGuard(new SimulationConfig { SafeMode = true, MaxRiskLevel = RiskLevel.Critical });
        var ok = guard.IsAllowed(Browse(url, RiskLevel.Low), out var reason);
        Assert.True(ok, reason);
    }

    [Fact]
    public void SafeMode_off_allows_arbitrary_url()
    {
        var guard = new SafetyGuard(new SimulationConfig { SafeMode = false, MaxRiskLevel = RiskLevel.Critical });
        var ok = guard.IsAllowed(Browse("https://evil.example.com/payload", RiskLevel.High), out _);
        Assert.True(ok);
    }

    [Fact]
    public void SafeMode_blocks_unlisted_command()
    {
        var guard = new SafetyGuard(new SimulationConfig { SafeMode = true, MaxRiskLevel = RiskLevel.Critical });
        Assert.Throws<SafetyViolationException>(() =>
            guard.Validate(Run("powershell.exe -Command Remove-Item C:\\ -Recurse")));
    }

    [Fact]
    public void SafeMode_allows_vetted_commands()
    {
        var guard = new SafetyGuard(new SimulationConfig { SafeMode = true, MaxRiskLevel = RiskLevel.Critical });
        foreach (var cmd in TestPayloads.AllSafeCommands)
        {
            Assert.True(guard.IsAllowed(Run(cmd), out var reason), $"{cmd}: {reason}");
        }
    }

    [Fact]
    public void Real_disable_defender_command_is_blocked_in_safemode()
    {
        var guard = new SafetyGuard(new SimulationConfig { SafeMode = true, MaxRiskLevel = RiskLevel.Critical });
        Assert.Throws<SafetyViolationException>(() =>
            guard.Validate(Run(TestPayloads.DisableDefenderRealtimeCommand, RiskLevel.Critical)));
    }
}
