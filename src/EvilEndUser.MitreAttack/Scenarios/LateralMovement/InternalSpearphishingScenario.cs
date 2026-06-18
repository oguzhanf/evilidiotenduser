using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;

namespace EvilEndUser.MitreAttack.Scenarios.LateralMovement;

/// <summary>
/// TA0008 Lateral Movement — emulates internal spearphishing from a compromised account. The harness
/// sends a Teams message (and optionally an email) to colleagues containing a "review this document"
/// lure pointing at a safe SmartScreen phishing demo URL. Exercises Defender for Office 365 Safe
/// Links for Teams and internal-phishing detections.
/// </summary>
public sealed class InternalSpearphishingScenario : ScenarioBase
{
    /// <inheritdoc />
    public override string Id => "lateral-movement.internal-spearphishing";

    /// <inheritdoc />
    public override string Name => "Internal Spearphishing via Teams";

    /// <inheritdoc />
    public override string Description =>
        "Compromised account sends colleagues a Teams message with a phishing link (safe demo URL).";

    /// <inheritdoc />
    public override MitreTactic Tactic => MitreTactic.LateralMovement;

    /// <inheritdoc />
    public override IReadOnlyList<MitreTechnique> Techniques => new[]
    {
        TechniqueCatalog.InternalSpearphishing,
        TechniqueCatalog.SpearphishingViaService,
    };

    /// <inheritdoc />
    public override RiskLevel MaxRiskLevel => RiskLevel.Medium;

    /// <inheritdoc />
    public override bool CanRun(IScenarioContext ctx)
        => base.CanRun(ctx) && ctx.Config.EnableTeams;

    /// <inheritdoc />
    public override async Task RunAsync(IScenarioContext ctx, CancellationToken ct = default)
    {
        ctx.Log("Compromised account is messaging colleagues to spread the phish.");

        // Open the real Teams desktop client first.
        await Do(ctx, Action(
            SimActionType.LaunchDesktopApp,
            "teams",
            RiskLevel.Safe,
            TechniqueCatalog.InternalSpearphishing,
            "Opening Teams to message a colleague."), ct);

        var phishUrl = TestPayloads.SmartScreenDemos
            .First(d => d.Name.Contains("phishing", StringComparison.OrdinalIgnoreCase)).Url;

        var recipient = ctx.Config.ExternalRecipients.FirstOrDefault()
                        ?? ctx.Config.SelfMailbox
                        ?? "colleague@contoso.com";

        await Do(ctx, Action(
            SimActionType.SendTeamsMessage,
            recipient,
            RiskLevel.Medium,
            TechniqueCatalog.InternalSpearphishing,
            "Sending a trusted-looking 'can you review this invoice?' message.",
            new Dictionary<string, string>
            {
                ["message"] = $"Hey! Can you review this invoice before I send it? {phishUrl}",
                ["link"] = phishUrl,
            }), ct);

        ctx.Log("Internal spearphishing scenario complete.");
    }
}
