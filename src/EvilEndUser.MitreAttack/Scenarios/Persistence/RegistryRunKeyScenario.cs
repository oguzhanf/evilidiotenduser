using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;

namespace EvilEndUser.MitreAttack.Scenarios.Persistence;

/// <summary>
/// TA0003 Persistence — emulates malware (run by the user) establishing logon persistence by writing
/// an HKCU "Run" key. The registry driver constrains this to a clearly-labelled, sim-specific value
/// pointing at a harmless command, and the run is fully reversible. This generates the autostart
/// telemetry that Defender for Endpoint flags as a persistence attempt.
/// </summary>
public sealed class RegistryRunKeyScenario : ScenarioBase
{
    /// <inheritdoc />
    public override string Id => "persistence.registry-run-key";

    /// <inheritdoc />
    public override string Name => "Registry Run-Key Persistence";

    /// <inheritdoc />
    public override string Description =>
        "Writes a clearly-labelled HKCU Run key (reversible) to emulate logon persistence.";

    /// <inheritdoc />
    public override MitreTactic Tactic => MitreTactic.Persistence;

    /// <inheritdoc />
    public override IReadOnlyList<MitreTechnique> Techniques => new[]
    {
        TechniqueCatalog.RegistryRunKeys,
    };

    /// <inheritdoc />
    public override RiskLevel MaxRiskLevel => RiskLevel.High;

    /// <inheritdoc />
    public override bool CanRun(IScenarioContext ctx)
        => base.CanRun(ctx) && ctx.Config.EnableProcessExecution;

    /// <inheritdoc />
    public override async Task RunAsync(IScenarioContext ctx, CancellationToken ct = default)
    {
        ctx.Log("Payload is installing itself to run at every logon.");

        await Do(ctx, Action(
            SimActionType.ModifyRegistry,
            @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run",
            RiskLevel.High,
            TechniqueCatalog.RegistryRunKeys,
            "Adding a Run key so the 'malware' survives reboots.",
            new Dictionary<string, string>
            {
                ["valueName"] = "EvilEndUserSimPersistence",
                ["valueData"] = "cmd.exe /c echo EvilEndUser simulation persistence",
            }), ct);

        ctx.Log("Registry Run-key persistence scenario complete (remember to clean up the value).",
            EventSeverity.Warning);
    }
}
