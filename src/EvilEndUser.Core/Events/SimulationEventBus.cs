using EvilEndUser.Core.Models;

namespace EvilEndUser.Core.Events;

/// <summary>
/// A simple in-process publish/subscribe bus for <see cref="SimulationEvent"/>s. The engine and
/// drivers publish; the UI and exporters subscribe. Thread-safe for concurrent publishers.
/// </summary>
public interface ISimulationEventBus
{
    /// <summary>Raised on every published event. Handlers should be fast and non-throwing.</summary>
    event Action<SimulationEvent>? EventPublished;

    /// <summary>Publishes an event to all subscribers.</summary>
    void Publish(SimulationEvent evt);

    /// <summary>An immutable snapshot of every event published so far.</summary>
    IReadOnlyList<SimulationEvent> History { get; }
}

/// <summary>Default thread-safe implementation of <see cref="ISimulationEventBus"/>.</summary>
public sealed class SimulationEventBus : ISimulationEventBus
{
    private readonly object _gate = new();
    private readonly List<SimulationEvent> _history = new();

    /// <inheritdoc />
    public event Action<SimulationEvent>? EventPublished;

    /// <inheritdoc />
    public void Publish(SimulationEvent evt)
    {
        lock (_gate)
        {
            _history.Add(evt);
        }

        EventPublished?.Invoke(evt);
    }

    /// <inheritdoc />
    public IReadOnlyList<SimulationEvent> History
    {
        get
        {
            lock (_gate)
            {
                return _history.ToArray();
            }
        }
    }
}
