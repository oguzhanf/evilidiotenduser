using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;

namespace EvilEndUser.MitreAttack.Scenarios.CredentialAccess;

/// <summary>
/// TA0006 Credential Access — emulates an MFA-fatigue ("push bombing") victim. Because actually
/// triggering MFA prompts requires the attacker side, this harness drives the observable user-side
/// behaviour: repeatedly visiting the AMTSO phishing-feature check and the SmartScreen "are you
/// sure" prompt, narrating an eventual "Approve". Useful for validating sign-in/risk alerts.
/// </summary>
public sealed class MfaFatigueScenario : ScenarioBase
{
    /// <inheritdoc />
    public override string Id => "credential-access.mfa-fatigue";

    /// <inheritdoc />
    public override string Name => "MFA Fatigue (Push Bombing)";

    /// <inheritdoc />
    public override string Description =>
        "User is bombarded with authentication prompts and eventually approves one.";

    /// <inheritdoc />
    public override MitreTactic Tactic => MitreTactic.CredentialAccess;

    /// <inheritdoc />
    public override IReadOnlyList<MitreTechnique> Techniques => new[]
    {
        TechniqueCatalog.MfaFatigue,
    };

    /// <inheritdoc />
    public override RiskLevel MaxRiskLevel => RiskLevel.Medium;

    /// <inheritdoc />
    public override async Task RunAsync(IScenarioContext ctx, CancellationToken ct = default)
    {
        var prompts = ctx.Random.Next(3, 7);
        ctx.Log($"User is receiving {prompts} unexpected authentication prompts.");

        var url = TestPayloads.SmartScreenDemos
            .First(d => d.Name.Contains("Is This Phishing", StringComparison.OrdinalIgnoreCase)).Url;

        for (var i = 0; i < prompts; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Do(ctx, Action(
                SimActionType.BrowseUrl,
                url,
                RiskLevel.Low,
                TechniqueCatalog.MfaFatigue,
                $"Prompt #{i + 1} popped up again; I'm getting annoyed."), ct);
        }

        ctx.Log("User tapped 'Approve' just to make the prompts stop.", Core.Models.EventSeverity.Telemetry);
    }
}
