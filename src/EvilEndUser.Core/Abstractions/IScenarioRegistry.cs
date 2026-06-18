using EvilEndUser.Core.Models;

namespace EvilEndUser.Core.Abstractions;

/// <summary>
/// A registry of all available <see cref="IScenario"/> implementations, grouped by tactic.
/// </summary>
public interface IScenarioRegistry
{
    /// <summary>Every registered scenario.</summary>
    IReadOnlyList<IScenario> All { get; }

    /// <summary>Returns the scenarios belonging to a given tactic.</summary>
    IReadOnlyList<IScenario> ForTactic(MitreTactic tactic);

    /// <summary>Finds a scenario by its stable id, or null.</summary>
    IScenario? FindById(string id);
}
