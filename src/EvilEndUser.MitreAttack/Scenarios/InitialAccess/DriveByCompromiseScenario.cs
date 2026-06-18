using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;

namespace EvilEndUser.MitreAttack.Scenarios.InitialAccess;

/// <summary>
/// TA0001 Initial Access — emulates a user browsing to a compromised / malvertising page. Uses
/// Microsoft's SmartScreen exploit and malware demonstration URLs, which are safe but trigger the
/// Edge SmartScreen block experience and Defender for Endpoint network protection.
/// </summary>
public sealed class DriveByCompromiseScenario : ScenarioBase
{
    /// <inheritdoc />
    public override string Id => "initial-access.drive-by-compromise";

    /// <inheritdoc />
    public override string Name => "Drive-by Compromise";

    /// <inheritdoc />
    public override string Description =>
        "User browses to malicious/exploit pages (Microsoft SmartScreen demo URLs) and a network-" +
        "protection test domain.";

    /// <inheritdoc />
    public override MitreTactic Tactic => MitreTactic.InitialAccess;

    /// <inheritdoc />
    public override IReadOnlyList<MitreTechnique> Techniques => new[]
    {
        TechniqueCatalog.DriveByCompromise,
    };

    /// <inheritdoc />
    public override RiskLevel MaxRiskLevel => RiskLevel.Medium;

    /// <inheritdoc />
    public override async Task RunAsync(IScenarioContext ctx, CancellationToken ct = default)
    {
        ctx.Log("User clicked an enticing ad and started browsing sketchy pages.");

        var targets = new[]
        {
            TestPayloads.SmartScreenDemos.First(d => d.Name.Contains("exploit", StringComparison.OrdinalIgnoreCase)).Url,
            TestPayloads.SmartScreenDemos.First(d => d.Name.Contains("malware", StringComparison.OrdinalIgnoreCase)).Url,
            TestPayloads.NetworkProtectionTestUrls[0],
        };

        foreach (var url in targets)
        {
            ct.ThrowIfCancellationRequested();
            await Do(ctx, Action(
                SimActionType.BrowseUrl,
                url,
                RiskLevel.Medium,
                TechniqueCatalog.DriveByCompromise,
                "The page promised a free prize, so I kept clicking."), ct);
            await ctx.ThinkAsync(ct);
        }

        ctx.Log("Drive-by compromise scenario complete.");
    }
}
