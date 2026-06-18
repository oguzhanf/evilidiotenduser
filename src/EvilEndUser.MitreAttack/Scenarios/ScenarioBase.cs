using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;

namespace EvilEndUser.MitreAttack.Scenarios;

/// <summary>
/// Base class for deterministic scenario harnesses. Provides convenience helpers for building and
/// executing <see cref="SimAction"/>s and a default <see cref="CanRun"/> implementation that checks
/// the configured risk ceiling.
/// </summary>
public abstract class ScenarioBase : IScenario
{
    /// <inheritdoc />
    public abstract string Id { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public abstract MitreTactic Tactic { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<MitreTechnique> Techniques { get; }

    /// <inheritdoc />
    public virtual RiskLevel MaxRiskLevel => RiskLevel.High;

    /// <inheritdoc />
    public virtual bool CanRun(IScenarioContext context) => context.Config.Permits(MaxRiskLevel);

    /// <inheritdoc />
    public abstract Task RunAsync(IScenarioContext context, CancellationToken ct = default);

    /// <summary>Builds a <see cref="SimAction"/> with the supplied fields.</summary>
    protected static SimAction Action(
        SimActionType type,
        string? target = null,
        RiskLevel risk = RiskLevel.Safe,
        MitreTechnique? technique = null,
        string? rationale = null,
        IReadOnlyDictionary<string, string>? parameters = null)
        => new()
        {
            Type = type,
            Target = target,
            RiskLevel = risk,
            Technique = technique,
            Rationale = rationale,
            Parameters = parameters ?? new Dictionary<string, string>(),
        };

    /// <summary>Executes a single action through the context (applies safety + telemetry).</summary>
    protected static Task<ActionResult> Do(IScenarioContext ctx, SimAction action, CancellationToken ct)
        => ctx.ExecuteAsync(action, ct);

    /// <summary>Picks a random element from a list using the context RNG.</summary>
    protected static T Pick<T>(IScenarioContext ctx, IReadOnlyList<T> items)
        => items[ctx.Random.Next(items.Count)];
}
