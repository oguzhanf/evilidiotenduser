using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;

namespace EvilEndUser.MitreAttack.Scenarios.Exfiltration;

/// <summary>
/// TA0010 Exfiltration — emulates a user (insider or compromised) self-exfiltrating data. The harness
/// sends an email to a configured external recipient with a GTUBE spam-test marker in the body (so
/// Exchange Online Protection / DLP can react) and then "uploads" a decoy file by browsing to a safe
/// network-protection test endpoint. Exercises Defender for Office 365 and DLP egress controls.
/// </summary>
public sealed class SelfExfiltrationScenario : ScenarioBase
{
    /// <inheritdoc />
    public override string Id => "exfiltration.self-exfil";

    /// <inheritdoc />
    public override string Name => "Self-Exfiltration to External Account";

    /// <inheritdoc />
    public override string Description =>
        "User emails sensitive data to a personal external address and uploads a file to the web.";

    /// <inheritdoc />
    public override MitreTactic Tactic => MitreTactic.Exfiltration;

    /// <inheritdoc />
    public override IReadOnlyList<MitreTechnique> Techniques => new[]
    {
        TechniqueCatalog.SelfExfiltration,
        TechniqueCatalog.ExfilToCloud,
    };

    /// <inheritdoc />
    public override RiskLevel MaxRiskLevel => RiskLevel.High;

    /// <inheritdoc />
    public override bool CanRun(IScenarioContext ctx)
        => base.CanRun(ctx) && (ctx.Config.EnableEmail || true);

    /// <inheritdoc />
    public override async Task RunAsync(IScenarioContext ctx, CancellationToken ct = default)
    {
        var external = ctx.Config.ExternalRecipients.FirstOrDefault() ?? "my.personal.account@example.com";
        ctx.Log($"User is forwarding work files to a personal address: {external}.");

        if (ctx.Config.EnableEmail)
        {
            await Do(ctx, Action(
                SimActionType.SendEmail,
                external,
                RiskLevel.High,
                TechniqueCatalog.SelfExfiltration,
                "I wanted to work on these from home, so I emailed them to myself.",
                new Dictionary<string, string>
                {
                    ["subject"] = "FW: Q4 numbers (do not share)",
                    // GTUBE marker guarantees an EOP spam/DLP reaction without real malicious content.
                    ["body"] = "Here are the confidential files for tonight.\n\n" + TestPayloads.GtubeString,
                    ["external"] = "true",
                }), ct);

            await ctx.ThinkAsync(ct);
        }

        // Emulate an upload to an external service via a safe network-protection test endpoint.
        await Do(ctx, Action(
            SimActionType.UploadToCloud,
            TestPayloads.NetworkProtectionTestUrls[0],
            RiskLevel.Medium,
            TechniqueCatalog.ExfilToCloud,
            "Then I dragged the archive into a free file-sharing site."), ct);

        ctx.Log("Self-exfiltration scenario complete.");
    }
}
