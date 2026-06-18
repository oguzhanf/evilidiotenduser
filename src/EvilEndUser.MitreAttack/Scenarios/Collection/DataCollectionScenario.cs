using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;

namespace EvilEndUser.MitreAttack.Scenarios.Collection;

/// <summary>
/// TA0009 Collection — emulates a payload gathering local data prior to exfiltration. The harness
/// drops a set of decoy "sensitive" documents into the working directory and then narrates archiving
/// them, producing file-creation telemetry without touching any of the user's real files.
/// </summary>
public sealed class DataCollectionScenario : ScenarioBase
{
    /// <inheritdoc />
    public override string Id => "collection.local-data";

    /// <inheritdoc />
    public override string Name => "Local Data Collection & Staging";

    /// <inheritdoc />
    public override string Description =>
        "Creates decoy sensitive documents and stages them into an archive folder for exfiltration.";

    /// <inheritdoc />
    public override MitreTactic Tactic => MitreTactic.Collection;

    /// <inheritdoc />
    public override IReadOnlyList<MitreTechnique> Techniques => new[]
    {
        TechniqueCatalog.FileDiscovery,
        TechniqueCatalog.ArchiveData,
    };

    /// <inheritdoc />
    public override RiskLevel MaxRiskLevel => RiskLevel.Low;

    /// <inheritdoc />
    public override bool CanRun(IScenarioContext ctx)
        => base.CanRun(ctx) && ctx.Config.EnableFileSystem;

    /// <inheritdoc />
    public override async Task RunAsync(IScenarioContext ctx, CancellationToken ct = default)
    {
        ctx.Log("Payload is hunting for 'sensitive' files to steal.");

        var stagingDir = Path.Combine(ctx.Config.WorkingDirectory, "EvilEndUser_Staging");
        var decoys = new[]
        {
            ("Customer_PII_Export.csv", "name,ssn,email\nJane Doe,000-00-0000,jane@contoso.com (DECOY DATA)"),
            ("Q4_Financials.txt", "CONFIDENTIAL — simulated financial projections (DECOY DATA)"),
            ("VPN_Credentials.txt", "host: vpn.contoso.com\nuser: simuser\npass: DECOY-NOT-REAL"),
        };

        foreach (var (name, content) in decoys)
        {
            ct.ThrowIfCancellationRequested();
            await Do(ctx, Action(
                SimActionType.WriteFile,
                Path.Combine(stagingDir, name),
                RiskLevel.Low,
                TechniqueCatalog.FileDiscovery,
                "Found a file that looks valuable; staging it.",
                new Dictionary<string, string> { ["content"] = content }), ct);
        }

        ctx.Log("Decoy data staged into archive folder (archive-via-utility emulated).",
            EventSeverity.Telemetry);
    }
}
