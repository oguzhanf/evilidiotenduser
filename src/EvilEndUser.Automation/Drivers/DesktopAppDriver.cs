using System.Diagnostics;
using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.Extensions.Logging;

namespace EvilEndUser.Automation.Drivers;

/// <summary>
/// Launches the genuine desktop applications a user interacts with and brings their windows to the
/// foreground using FlaUI (UI Automation). Starting the real processes (outlook.exe, ms-teams,
/// msedge.exe, explorer.exe, …) produces authentic process-creation telemetry for Defender for
/// Endpoint — exactly what a "stupid end user" opening their apps would generate.
/// </summary>
public sealed class DesktopAppDriver : IDesktopDriver
{
    private readonly ILogger<DesktopAppDriver>? _logger;

    /// <summary>Friendly name → launch strategy. URIs use the shell so installed UWP/Win32 apps both work.</summary>
    private static readonly IReadOnlyDictionary<string, string> KnownApps =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["outlook"] = "outlook.exe",
            ["teams"] = "ms-teams:",
            ["edge"] = "msedge.exe",
            ["chrome"] = "chrome.exe",
            ["explorer"] = "explorer.exe",
            ["notepad"] = "notepad.exe",
            ["word"] = "winword.exe",
            ["excel"] = "excel.exe",
        };

    /// <summary>Creates the driver.</summary>
    public DesktopAppDriver(ILogger<DesktopAppDriver>? logger = null) => _logger = logger;

    /// <inheritdoc />
    public string Name => "Desktop";

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(OperatingSystem.IsWindows());

    /// <inheritdoc />
    public async Task<ActionResult> LaunchAppAsync(SimAction action, CancellationToken ct = default)
    {
        var start = DateTimeOffset.Now;
        var appName = (action.Target ?? string.Empty).Trim();

        if (!KnownApps.TryGetValue(appName, out var launchTarget))
        {
            // Allow an explicit executable/URI to be passed through.
            launchTarget = appName;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = launchTarget,
                UseShellExecute = true,
            };
            var process = Process.Start(psi);

            // Best-effort: focus the launched window via UI Automation so it looks like a real user.
            var focused = await TryFocusAsync(process, appName, ct);

            _logger?.LogInformation("Launched desktop app {App} ({Target})", appName, launchTarget);
            return ActionResult.Ok(action,
                $"Launched '{appName}'{(focused ? " and focused its window" : "")}.",
                DateTimeOffset.Now - start);
        }
        catch (Exception ex)
        {
            return ActionResult.Ok(action,
                $"Launch of '{appName}' attempted: {ex.Message} (app may not be installed on this endpoint).",
                DateTimeOffset.Now - start);
        }
    }

    private async Task<bool> TryFocusAsync(Process? process, string appName, CancellationToken ct)
    {
        try
        {
            // Give the app a moment to create its main window.
            await Task.Delay(2500, ct);

            using var automation = new UIA3Automation();

            // Prefer the process we started; fall back to a same-named process (UWP shells re-parent).
            var target = process;
            if (target is null || target.HasExited || target.MainWindowHandle == nint.Zero)
            {
                target = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(appName))
                    .FirstOrDefault(p => p.MainWindowHandle != nint.Zero);
            }

            if (target is null || target.MainWindowHandle == nint.Zero)
            {
                return false;
            }

            var window = automation.FromHandle(target.MainWindowHandle)?.AsWindow();
            if (window is null)
            {
                return false;
            }

            window.Focus();
            window.SetForeground();
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Could not focus window for {App}", appName);
            return false;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
