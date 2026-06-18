using EvilEndUser.Core.Models;

namespace EvilEndUser.Core.Abstractions;

/// <summary>
/// A self-contained emulation of a MITRE ATT&amp;CK technique (or short chain of techniques) carried
/// out as a sequence of user actions. Scenarios are deterministic harnesses; the optional LLM brain
/// can additionally improvise actions around them.
/// </summary>
public interface IScenario
{
    /// <summary>Stable identifier, e.g. <c>initial-access.spearphishing-link</c>.</summary>
    string Id { get; }

    /// <summary>Human-readable name shown in the UI.</summary>
    string Name { get; }

    /// <summary>One-line description of what the scenario does.</summary>
    string Description { get; }

    /// <summary>The primary tactic this scenario exercises.</summary>
    MitreTactic Tactic { get; }

    /// <summary>The MITRE techniques this scenario emulates.</summary>
    IReadOnlyList<MitreTechnique> Techniques { get; }

    /// <summary>The highest risk level any action in this scenario can reach.</summary>
    RiskLevel MaxRiskLevel { get; }

    /// <summary>
    /// Returns true when this scenario can run given the current configuration / available drivers.
    /// </summary>
    bool CanRun(IScenarioContext context);

    /// <summary>Executes the scenario, funnelling every action through <see cref="IScenarioContext.ExecuteAsync"/>.</summary>
    Task RunAsync(IScenarioContext context, CancellationToken ct = default);
}
