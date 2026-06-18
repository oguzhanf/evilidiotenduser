using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;

namespace EvilEndUser.Tests;

/// <summary>A browser driver test double that records actions instead of touching a real browser.</summary>
public sealed class FakeBrowserDriver : IBrowserDriver
{
    public List<SimAction> Navigations { get; } = new();

    public string Name => "FakeBrowser";
    public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(true);

    public Task<ActionResult> NavigateAsync(SimAction action, CancellationToken ct = default)
    {
        Navigations.Add(action);
        return Task.FromResult(ActionResult.Ok(action, "navigated"));
    }

    public Task<ActionResult> DownloadAsync(SimAction action, CancellationToken ct = default)
        => Task.FromResult(ActionResult.Ok(action, "downloaded"));

    public Task<ActionResult> SubmitFormAsync(SimAction action, CancellationToken ct = default)
        => Task.FromResult(ActionResult.Ok(action, "submitted"));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>A filesystem driver test double.</summary>
public sealed class FakeFileSystemDriver : IFileSystemDriver
{
    public List<SimAction> Writes { get; } = new();

    public string Name => "FakeFs";
    public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(true);

    public Task<ActionResult> WriteFileAsync(SimAction action, CancellationToken ct = default)
    {
        Writes.Add(action);
        return Task.FromResult(ActionResult.Ok(action, "wrote", default, new[] { action.Target ?? "f" }));
    }

    public Task<ActionResult> OpenFileAsync(SimAction action, CancellationToken ct = default)
        => Task.FromResult(ActionResult.Ok(action, "opened"));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>A process driver test double.</summary>
public sealed class FakeProcessDriver : IProcessDriver
{
    public List<SimAction> Runs { get; } = new();

    public string Name => "FakeProc";
    public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(true);

    public Task<ActionResult> RunAsync(SimAction action, CancellationToken ct = default)
    {
        Runs.Add(action);
        return Task.FromResult(ActionResult.Ok(action, "ran"));
    }

    public Task<ActionResult> ModifyRegistryAsync(SimAction action, CancellationToken ct = default)
        => Task.FromResult(ActionResult.Ok(action, "regset"));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
