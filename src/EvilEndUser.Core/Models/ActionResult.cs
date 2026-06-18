namespace EvilEndUser.Core.Models;

/// <summary>
/// The outcome of executing a single <see cref="SimAction"/>.
/// </summary>
public sealed class ActionResult
{
    /// <summary>The action that was attempted.</summary>
    public required SimAction Action { get; init; }

    /// <summary>True when the action ran to completion without throwing.</summary>
    public required bool Success { get; init; }

    /// <summary>Human-readable description of what happened.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Exception detail when <see cref="Success"/> is false.</summary>
    public string? Error { get; init; }

    /// <summary>When the action started.</summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.Now;

    /// <summary>How long the action took to execute.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Optional artefacts produced (e.g. dropped file path, screenshot path).</summary>
    public IReadOnlyList<string> Artifacts { get; init; } = Array.Empty<string>();

    /// <summary>Creates a successful result.</summary>
    public static ActionResult Ok(SimAction action, string message = "", TimeSpan duration = default, IReadOnlyList<string>? artifacts = null)
        => new() { Action = action, Success = true, Message = message, Duration = duration, Artifacts = artifacts ?? Array.Empty<string>() };

    /// <summary>Creates a failed result.</summary>
    public static ActionResult Fail(SimAction action, string message, string? error = null, TimeSpan duration = default)
        => new() { Action = action, Success = false, Message = message, Error = error, Duration = duration };
}
