using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;

namespace EvilEndUser.App.Models;

/// <summary>
/// A display-friendly projection of an <see cref="IScenario"/> for binding in the Scenarios page.
/// </summary>
public sealed class ScenarioCard
{
    private readonly IScenario _scenario;

    /// <summary>Creates a card wrapping a scenario.</summary>
    public ScenarioCard(IScenario scenario) => _scenario = scenario;

    /// <summary>The scenario id (used to launch it).</summary>
    public string Id => _scenario.Id;

    /// <summary>Display name.</summary>
    public string Name => _scenario.Name;

    /// <summary>Description.</summary>
    public string Description => _scenario.Description;

    /// <summary>The owning tactic.</summary>
    public MitreTactic Tactic => _scenario.Tactic;

    /// <summary>The tactic's display name and ATT&amp;CK id.</summary>
    public string TacticHeader
    {
        get
        {
            var info = MitreTactics.Get(_scenario.Tactic);
            return $"{info.Name} ({info.Id})";
        }
    }

    /// <summary>The highest risk the scenario reaches.</summary>
    public RiskLevel MaxRiskLevel => _scenario.MaxRiskLevel;

    /// <summary>The highest risk as a display string.</summary>
    public string RiskText => _scenario.MaxRiskLevel.ToString();

    /// <summary>Comma-separated technique IDs.</summary>
    public string TechniquesText => string.Join("  ·  ", _scenario.Techniques.Select(t => $"{t.Id} {t.Name}"));
}
