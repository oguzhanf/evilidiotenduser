using System.Diagnostics;
using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace EvilEndUser.Automation.Drivers;

/// <summary>
/// Runs processes / shell commands (PowerShell, cmd) and performs current-user registry edits,
/// emulating "ClickFix" execution and Run-key persistence. The driver captures stdout/stderr so the
/// operator can see what happened. Registry writes are constrained to the current-user hive.
/// </summary>
public sealed class ProcessDriver : IProcessDriver
{
    private readonly ILogger<ProcessDriver>? _logger;

    /// <summary>Creates the driver.</summary>
    public ProcessDriver(ILogger<ProcessDriver>? logger = null) => _logger = logger;

    /// <inheritdoc />
    public string Name => "Process";

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(OperatingSystem.IsWindows());

    /// <inheritdoc />
    public async Task<ActionResult> RunAsync(SimAction action, CancellationToken ct = default)
    {
        var start = DateTimeOffset.Now;
        var commandLine = action.Target ?? string.Empty;
        try
        {
            var (fileName, arguments) = SplitCommandLine(commandLine);
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi)
                ?? throw new InvalidOperationException("Process failed to start.");

            var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = proc.StandardError.ReadToEndAsync(ct);

            // Bound the run so a hung process never stalls the simulation.
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));
            try
            {
                await proc.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                TryKill(proc);
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var snippet = Truncate((stdout + stderr).Trim(), 400);

            _logger?.LogInformation("Ran '{Cmd}' (exit {Exit})", commandLine, proc.HasExited ? proc.ExitCode : -1);
            return ActionResult.Ok(action,
                $"Ran '{fileName}' (exit {(proc.HasExited ? proc.ExitCode : -1)}). Output: {snippet}",
                DateTimeOffset.Now - start);
        }
        catch (Exception ex)
        {
            // A command blocked by Defender/ASR is a successful test outcome, not a driver failure.
            return ActionResult.Ok(action,
                $"Command attempted but did not complete: {ex.Message} (possibly blocked by Defender/ASR).",
                DateTimeOffset.Now - start);
        }
    }

    /// <inheritdoc />
    public Task<ActionResult> ModifyRegistryAsync(SimAction action, CancellationToken ct = default)
    {
        var start = DateTimeOffset.Now;
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                return Task.FromResult(ActionResult.Fail(action, "Registry edits require Windows."));
            }

            var keyPath = action.Target ?? string.Empty;
            if (!keyPath.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(ActionResult.Fail(action,
                    "Only current-user (HKCU) registry edits are permitted by the simulator."));
            }

            var subKey = keyPath["HKCU\\".Length..];
            var valueName = action.Param("valueName", "EvilEndUserSimTest");
            var valueData = action.Param("valueData", "cmd.exe /c echo EvilEndUser simulation");

            using var key = Registry.CurrentUser.CreateSubKey(subKey, writable: true)
                ?? throw new InvalidOperationException($"Could not open HKCU\\{subKey}.");
            key.SetValue(valueName, valueData, RegistryValueKind.String);

            _logger?.LogInformation("Set HKCU\\{Sub}\\{Value}", subKey, valueName);
            return Task.FromResult(ActionResult.Ok(action,
                $"Set HKCU\\{subKey}\\{valueName}. Remove with: reg delete \"HKCU\\{subKey}\" /v {valueName} /f",
                DateTimeOffset.Now - start));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.Ok(action,
                $"Registry edit attempted: {ex.Message} (possibly blocked by Defender).",
                DateTimeOffset.Now - start));
        }
    }

    private static void TryKill(Process proc)
    {
        try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); }
        catch { /* best effort */ }
    }

    /// <summary>Splits a command line into executable + argument string (handles a quoted exe).</summary>
    internal static (string FileName, string Arguments) SplitCommandLine(string commandLine)
    {
        commandLine = commandLine.Trim();
        if (commandLine.StartsWith('"'))
        {
            var end = commandLine.IndexOf('"', 1);
            if (end > 0)
            {
                return (commandLine[1..end], commandLine[(end + 1)..].TrimStart());
            }
        }

        var spaceIdx = commandLine.IndexOf(' ');
        return spaceIdx < 0
            ? (commandLine, string.Empty)
            : (commandLine[..spaceIdx], commandLine[(spaceIdx + 1)..]);
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
