using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;

namespace EvilEndUser.MitreAttack.Scenarios.InitialAccess;

/// <summary>
/// TA0001 Initial Access — emulates a naive user receiving a phishing email and clicking the link,
/// landing on a (safe, Microsoft-hosted) SmartScreen phishing demonstration page, then typing
/// credentials into the fake form. Exercises Defender for Office 365 Safe Links and Edge SmartScreen.
/// </summary>
public sealed class SpearphishingLinkScenario : ScenarioBase
{
    /// <inheritdoc />
    public override string Id => "initial-access.spearphishing-link";

    /// <inheritdoc />
    public override string Name => "Spearphishing Link → Credential Entry";

    /// <inheritdoc />
    public override string Description =>
        "User clicks a phishing link and submits credentials on a fake login page " +
        "(uses Microsoft's SmartScreen phishing demo URL).";

    /// <inheritdoc />
    public override MitreTactic Tactic => MitreTactic.InitialAccess;

    /// <inheritdoc />
    public override IReadOnlyList<MitreTechnique> Techniques => new[]
    {
        TechniqueCatalog.SpearphishingLink,
        TechniqueCatalog.PhishingForInfoLink,
    };

    /// <inheritdoc />
    public override RiskLevel MaxRiskLevel => RiskLevel.High;

    /// <inheritdoc />
    public override async Task RunAsync(IScenarioContext ctx, CancellationToken ct = default)
    {
        ctx.Log("User received an 'IT Security Alert' email and is curious about the link.");

        // The naive user clicks the phishing link from the email (safe demo page).
        var phishUrl = TestPayloads.SmartScreenDemos
            .First(d => d.Name.Contains("phishing", StringComparison.OrdinalIgnoreCase)).Url;

        await Do(ctx, Action(
            SimActionType.ClickEmailLink,
            phishUrl,
            RiskLevel.High,
            TechniqueCatalog.SpearphishingLink,
            "The email looked official and warned my account would be locked."), ct);

        await ctx.ThinkAsync(ct);

        // The user then types credentials into the fake login form.
        await Do(ctx, Action(
            SimActionType.SubmitWebForm,
            phishUrl,
            RiskLevel.High,
            TechniqueCatalog.PhishingForInfoLink,
            "I entered my username and password to 'verify' my account.",
            new Dictionary<string, string>
            {
                ["username"] = ctx.Config.SelfMailbox ?? "victim@contoso.com",
                ["password"] = "Sim-NotARealPassword!" + ctx.Random.Next(1000, 9999),
            }), ct);

        ctx.Log("Spearphishing-link scenario complete.");
    }
}
