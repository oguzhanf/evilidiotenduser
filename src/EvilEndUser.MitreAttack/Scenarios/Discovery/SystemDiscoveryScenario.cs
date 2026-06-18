using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;

namespace EvilEndUser.MitreAttack.Scenarios.Discovery;

/// <summary>
/// TA0007 Discovery — emulates payload-driven host reconnaissance using built-in Windows tooling
/// (whoami, systeminfo, ipconfig, net user). These commands are benign on their own but, run in a
/// burst, generate the discovery telemetry Defender for Endpoint correlates into an incident.
/// </summary>
public sealed class SystemDiscoveryScenario : ScenarioBase
{
    /// <inheritdoc />
    public override string Id => "discovery.system-recon";

    /// <inheritdoc />
    public override string Name => "System & Account Discovery";

    /// <inheritdoc />
    public override string Description =>
        "Runs a burst of built-in reconnaissance commands (whoami, systeminfo, ipconfig, net user).";

    /// <inheritdoc />
    public override MitreTactic Tactic => MitreTactic.Discovery;

    /// <inheritdoc />
    public override IReadOnlyList<MitreTechnique> Techniques => new[]
    {
        TechniqueCatalog.SystemInfoDiscovery,
        TechniqueCatalog.FileDiscovery,
    };

    /// <inheritdoc />
    public override RiskLevel MaxRiskLevel => RiskLevel.Medium;

    /// <inheritdoc />
    public override bool CanRun(IScenarioContext ctx)
        => base.CanRun(ctx) && ctx.Config.EnableProcessExecution;

    /// <inheritdoc />
    public override async Task RunAsync(IScenarioContext ctx, CancellationToken ct = default)
    {
        ctx.Log("Dropped payload is enumerating the host.");

        foreach (var command in TestPayloads.SafeReconCommands)
        {
            ct.ThrowIfCancellationRequested();
            await Do(ctx, Action(
                SimActionType.RunProcess,
                command,
                RiskLevel.Medium,
                TechniqueCatalog.SystemInfoDiscovery,
                "Fingerprinting the machine before deciding what to steal."), ct);
        }

        ctx.Log("System & account discovery scenario complete.");
    }
}
