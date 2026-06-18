using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;

namespace EvilEndUser.MitreAttack.Scenarios.Execution;

/// <summary>
/// TA0002 Execution — emulates the "ClickFix" social-engineering pattern where a user is tricked by
/// a fake CAPTCHA/error into pasting a command into the Run dialog or PowerShell. Runs one of the
/// vetted, inert benign-but-suspicious commands (e.g. an encoded PowerShell that just prints text)
/// to exercise Defender for Endpoint behavioural/encoded-command detections without doing harm.
/// </summary>
public sealed class ClickFixPowerShellScenario : ScenarioBase
{
    /// <inheritdoc />
    public override string Id => "execution.clickfix-powershell";

    /// <inheritdoc />
    public override string Name => "ClickFix PowerShell Execution";

    /// <inheritdoc />
    public override string Description =>
        "User pastes and runs an encoded PowerShell command after seeing a fake 'fix this error' prompt.";

    /// <inheritdoc />
    public override MitreTactic Tactic => MitreTactic.Execution;

    /// <inheritdoc />
    public override IReadOnlyList<MitreTechnique> Techniques => new[]
    {
        TechniqueCatalog.PowerShell,
    };

    /// <inheritdoc />
    public override RiskLevel MaxRiskLevel => RiskLevel.High;

    /// <inheritdoc />
    public override bool CanRun(IScenarioContext ctx)
        => base.CanRun(ctx) && ctx.Config.EnableProcessExecution;

    /// <inheritdoc />
    public override async Task RunAsync(IScenarioContext ctx, CancellationToken ct = default)
    {
        ctx.Log("User saw a fake 'Verify you are human — run this to continue' prompt.");

        // SafeMode requires the command come verbatim from the vetted allow-list.
        var command = TestPayloads.BenignSuspiciousCommands
            .First(c => c.StartsWith("powershell", StringComparison.OrdinalIgnoreCase));

        await Do(ctx, Action(
            SimActionType.RunProcess,
            command,
            RiskLevel.High,
            TechniqueCatalog.PowerShell,
            "The website told me to press Win+R and paste this to fix the error."), ct);

        ctx.Log("ClickFix PowerShell scenario complete.");
    }
}
