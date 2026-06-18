using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;

namespace EvilEndUser.MitreAttack.Scenarios.Execution;

/// <summary>
/// TA0002 Execution — emulates a user downloading and "opening" a malicious attachment. The harness
/// drops the harmless EICAR anti-malware test file (masqueraded with a deceptive name) into the
/// Downloads folder and then opens it, reliably triggering Microsoft Defender Antivirus.
/// </summary>
public sealed class MaliciousFileScenario : ScenarioBase
{
    /// <inheritdoc />
    public override string Id => "execution.malicious-file";

    /// <inheritdoc />
    public override string Name => "Malicious File Execution (EICAR)";

    /// <inheritdoc />
    public override string Description =>
        "Drops the EICAR test file disguised as an invoice and opens it, triggering Defender AV.";

    /// <inheritdoc />
    public override MitreTactic Tactic => MitreTactic.Execution;

    /// <inheritdoc />
    public override IReadOnlyList<MitreTechnique> Techniques => new[]
    {
        TechniqueCatalog.UserExecutionFile,
        TechniqueCatalog.Masquerading,
    };

    /// <inheritdoc />
    public override RiskLevel MaxRiskLevel => RiskLevel.High;

    /// <inheritdoc />
    public override bool CanRun(IScenarioContext ctx)
        => base.CanRun(ctx) && ctx.Config.EnableFileSystem;

    /// <inheritdoc />
    public override async Task RunAsync(IScenarioContext ctx, CancellationToken ct = default)
    {
        // Masquerade the file name (T1036.005) as something the user would eagerly open.
        var lureNames = new[] { "Invoice_2026_Q4.pdf.txt", "PaySlip_December.docx.txt", "Refund_Details.txt" };
        var fileName = Pick(ctx, lureNames);
        var path = Path.Combine(ctx.Config.WorkingDirectory, fileName);

        ctx.Log($"User saved an email attachment '{fileName}' to Downloads.");

        // Open the real Outlook desktop client first (process telemetry for MDE).
        await Do(ctx, Action(
            SimActionType.LaunchDesktopApp,
            "outlook",
            RiskLevel.Safe,
            TechniqueCatalog.SpearphishingAttachment,
            "Opening Outlook to read the invoice email."), ct);

        var write = await Do(ctx, Action(
            SimActionType.WriteFile,
            path,
            RiskLevel.High,
            TechniqueCatalog.UserExecutionFile,
            "It looked like an invoice from a supplier, so I saved it.",
            new Dictionary<string, string>
            {
                ["content"] = TestPayloads.EicarString,
            }), ct);

        await ctx.ThinkAsync(ct);

        // Open / double-click it (T1204.002). Defender typically quarantines on write already.
        await Do(ctx, Action(
            SimActionType.OpenFile,
            write.Artifacts.Count > 0 ? write.Artifacts[0] : path,
            RiskLevel.High,
            TechniqueCatalog.Masquerading,
            "I double-clicked it to read the invoice."), ct);

        ctx.Log("Malicious-file execution scenario complete.");
    }
}
