using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;

namespace EvilEndUser.MitreAttack.Scenarios.DefenseImpairment;

/// <summary>
/// TA0112 Defense Impairment — emulates a user being talked into disabling their antivirus.
/// <para>
/// In <see cref="SimulationConfig.SafeMode"/> (default) the scenario only runs a read-only Defender
/// status probe and narrates the social-engineering, so nothing is actually disabled. When SafeMode
/// is OFF <i>and</i> the risk ceiling permits <see cref="RiskLevel.Critical"/>, it attempts the real
/// <c>Set-MpPreference -DisableRealtimeMonitoring</c> command, which on an E5 endpoint with Tamper
/// Protection is blocked and raises a high-value alert.
/// </para>
/// </summary>
public sealed class DisableDefenderScenario : ScenarioBase
{
    /// <inheritdoc />
    public override string Id => "defense-impairment.disable-defender";

    /// <inheritdoc />
    public override string Name => "Disable Antivirus (Social Engineering)";

    /// <inheritdoc />
    public override string Description =>
        "User is told by fake 'IT' to turn off Defender. Safe by default; really attempts it only when SafeMode is off.";

    /// <inheritdoc />
    public override MitreTactic Tactic => MitreTactic.DefenseImpairment;

    /// <inheritdoc />
    public override IReadOnlyList<MitreTechnique> Techniques => new[]
    {
        TechniqueCatalog.DisableTools,
    };

    /// <inheritdoc />
    public override RiskLevel MaxRiskLevel => RiskLevel.Critical;

    /// <inheritdoc />
    public override bool CanRun(IScenarioContext ctx)
        => ctx.Config.EnableProcessExecution; // risk gating handled per-action below

    /// <inheritdoc />
    public override async Task RunAsync(IScenarioContext ctx, CancellationToken ct = default)
    {
        ctx.Log("A fake 'IT helpdesk' caller is instructing the user to turn off antivirus.");

        // Always-safe step: the user 'checks' their AV status first.
        await Do(ctx, Action(
            SimActionType.RunProcess,
            TestPayloads.SafeDefenderProbeCommands[0],
            RiskLevel.Medium,
            TechniqueCatalog.DisableTools,
            "The caller asked me to open PowerShell and check my antivirus."), ct);

        await ctx.ThinkAsync(ct);

        if (ctx.Config.SafeMode || !ctx.Config.Permits(RiskLevel.Critical))
        {
            ctx.Log("SafeMode/risk-ceiling active — the real disable command is NOT executed; " +
                    "emulating the attempt only.", EventSeverity.Warning);
            return;
        }

        // Real attempt — Tamper Protection should block this and alert.
        await Do(ctx, Action(
            SimActionType.RunProcess,
            TestPayloads.DisableDefenderRealtimeCommand,
            RiskLevel.Critical,
            TechniqueCatalog.DisableTools,
            "The caller told me this command would 'let the update install'."), ct);

        ctx.Log("Disable-Defender scenario complete.");
    }
}
