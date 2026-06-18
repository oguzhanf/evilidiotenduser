using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.MitreAttack.Scenarios.Collection;
using EvilEndUser.MitreAttack.Scenarios.CredentialAccess;
using EvilEndUser.MitreAttack.Scenarios.DefenseImpairment;
using EvilEndUser.MitreAttack.Scenarios.Discovery;
using EvilEndUser.MitreAttack.Scenarios.Execution;
using EvilEndUser.MitreAttack.Scenarios.Exfiltration;
using EvilEndUser.MitreAttack.Scenarios.Impact;
using EvilEndUser.MitreAttack.Scenarios.InitialAccess;
using EvilEndUser.MitreAttack.Scenarios.LateralMovement;
using EvilEndUser.MitreAttack.Scenarios.Persistence;

namespace EvilEndUser.MitreAttack;

/// <summary>
/// Default <see cref="IScenarioRegistry"/> containing every built-in MITRE ATT&amp;CK scenario,
/// ordered along the kill chain. The WinUI app and the engine consume this to populate the UI and
/// run chains.
/// </summary>
public sealed class ScenarioRegistry : IScenarioRegistry
{
    /// <inheritdoc />
    public IReadOnlyList<IScenario> All { get; }

    /// <summary>Creates the registry and instantiates every built-in scenario.</summary>
    public ScenarioRegistry()
    {
        All = new IScenario[]
        {
            // TA0001 Initial Access
            new SpearphishingLinkScenario(),
            new DriveByCompromiseScenario(),
            // TA0002 Execution
            new MaliciousFileScenario(),
            new ClickFixPowerShellScenario(),
            // TA0003 Persistence
            new RegistryRunKeyScenario(),
            // TA0112 Defense Impairment
            new DisableDefenderScenario(),
            // TA0006 Credential Access
            new MfaFatigueScenario(),
            // TA0007 Discovery
            new SystemDiscoveryScenario(),
            // TA0008 Lateral Movement
            new InternalSpearphishingScenario(),
            // TA0009 Collection
            new DataCollectionScenario(),
            // TA0010 Exfiltration
            new SelfExfiltrationScenario(),
            // TA0040 Impact
            new BecFraudScenario(),
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<IScenario> ForTactic(MitreTactic tactic)
        => All.Where(s => s.Tactic == tactic).ToList();

    /// <inheritdoc />
    public IScenario? FindById(string id)
        => All.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns a curated, kill-chain-ordered chain emulating a full intrusion from a phishing email
    /// through to impact. Used by the "Run full kill chain" action in the UI.
    /// </summary>
    public IReadOnlyList<IScenario> FullKillChain() => new IScenario[]
    {
        FindBy<SpearphishingLinkScenario>(),
        FindBy<MaliciousFileScenario>(),
        FindBy<RegistryRunKeyScenario>(),
        FindBy<DisableDefenderScenario>(),
        FindBy<SystemDiscoveryScenario>(),
        FindBy<DataCollectionScenario>(),
        FindBy<SelfExfiltrationScenario>(),
        FindBy<BecFraudScenario>(),
    };

    private T FindBy<T>() where T : IScenario => All.OfType<T>().First();
}
