using EvilEndUser.Core.Models;

namespace EvilEndUser.Core.Abstractions;

/// <summary>
/// The LLM-backed "brain" that drives the simulated user in a non-deterministic way. Given the
/// current situation it proposes the next action(s) to take, flavoured by the configured persona.
/// </summary>
public interface IAgentBrain
{
    /// <summary>True when an LLM backend is configured and reachable.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Asks the brain to decide the next action given the current context and a situation summary
    /// (e.g. "You just received an email titled 'Your Q4 Pay Slip' with a link.").
    /// Implementations must only ever return actions the <c>SafetyGuard</c> will accept.
    /// </summary>
    Task<SimAction> DecideNextActionAsync(
        IScenarioContext context,
        string situation,
        CancellationToken ct = default);

    /// <summary>
    /// Runs a free-form, open-ended "wander" loop where the brain repeatedly decides and executes
    /// actions until <paramref name="maxSteps"/> is reached or cancellation is requested.
    /// </summary>
    Task WanderAsync(
        IScenarioContext context,
        int maxSteps,
        CancellationToken ct = default);
}
