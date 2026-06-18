using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;

namespace EvilEndUser.MitreAttack.Scenarios.Impact;

/// <summary>
/// TA0040 Impact — emulates Business Email Compromise (BEC) financial fraud. From the (notionally
/// compromised) mailbox, the harness sends a fraudulent wire-transfer request to an external
/// recipient, including a GTUBE marker so Exchange Online Protection reacts. Exercises Defender for
/// Office 365 impersonation/BEC detections and outbound DLP.
/// </summary>
public sealed class BecFraudScenario : ScenarioBase
{
    /// <inheritdoc />
    public override string Id => "impact.bec-fraud";

    /// <inheritdoc />
    public override string Name => "Business Email Compromise (Wire Fraud)";

    /// <inheritdoc />
    public override string Description =>
        "Sends a fraudulent wire-transfer request from the mailbox to an external party.";

    /// <inheritdoc />
    public override MitreTactic Tactic => MitreTactic.Impact;

    /// <inheritdoc />
    public override IReadOnlyList<MitreTechnique> Techniques => new[]
    {
        TechniqueCatalog.FinancialTheft,
    };

    /// <inheritdoc />
    public override RiskLevel MaxRiskLevel => RiskLevel.High;

    /// <inheritdoc />
    public override bool CanRun(IScenarioContext ctx)
        => base.CanRun(ctx) && ctx.Config.EnableEmail;

    /// <inheritdoc />
    public override async Task RunAsync(IScenarioContext ctx, CancellationToken ct = default)
    {
        var external = ctx.Config.ExternalRecipients.FirstOrDefault() ?? "cfo.external@example.com";
        ctx.Log("Attacker controlling the mailbox is sending fake wire-transfer instructions.");

        await Do(ctx, Action(
            SimActionType.SendEmail,
            external,
            RiskLevel.High,
            TechniqueCatalog.FinancialTheft,
            "Urgent vendor payment change — classic BEC lure.",
            new Dictionary<string, string>
            {
                ["subject"] = "URGENT: Updated wire instructions for today's payment",
                ["body"] = "Please process the attached vendor payment to the new account below " +
                           "before 3pm. Keep this confidential.\n\n" + TestPayloads.GtubeString,
                ["external"] = "true",
            }), ct);

        ctx.Log("BEC fraud scenario complete.");
    }
}
