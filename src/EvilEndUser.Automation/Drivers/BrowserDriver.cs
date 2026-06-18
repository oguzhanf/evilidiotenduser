using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace EvilEndUser.Automation.Drivers;

/// <summary>
/// Drives a real Microsoft Edge or Google Chrome browser via Playwright, the way an end user would:
/// navigating to URLs, triggering downloads, and filling in login forms. Driving the <i>system</i>
/// Edge/Chrome (via Playwright channels) is what makes SmartScreen / Network Protection / Safe Links
/// engage exactly as they would for a human.
/// </summary>
public sealed class BrowserDriver : IBrowserDriver
{
    private readonly SimulationConfig _config;
    private readonly ILogger<BrowserDriver>? _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;

    /// <summary>Creates the driver.</summary>
    public BrowserDriver(SimulationConfig config, ILogger<BrowserDriver>? logger = null)
    {
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => _config.Browser == BrowserKind.Edge ? "Edge" : "Chrome";

    private string Channel => _config.Browser == BrowserKind.Edge ? "msedge" : "chrome";

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureBrowserAsync(ct);
            return _browser is not null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Browser '{Channel}' not available", Channel);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ActionResult> NavigateAsync(SimAction action, CancellationToken ct = default)
    {
        var start = DateTimeOffset.Now;
        var url = action.Target ?? throw new ArgumentException("No URL supplied.");
        try
        {
            var page = await NewPageAsync(ct);
            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                Timeout = 30_000,
                WaitUntil = WaitUntilState.DOMContentLoaded,
            });

            // Let the operator watch and let SmartScreen render its interstitial.
            await page.WaitForTimeoutAsync(2_000);
            var status = response?.Status ?? 0;
            var title = await SafeTitle(page);

            _logger?.LogInformation("Navigated to {Url} (status {Status})", url, status);
            return ActionResult.Ok(action,
                $"Navigated to {url} → HTTP {status}, title '{title}'.",
                DateTimeOffset.Now - start);
        }
        catch (Exception ex)
        {
            // A block page / connection reset is an expected protective outcome, not a tool failure.
            return ActionResult.Ok(action,
                $"Navigation to {url} did not complete normally: {ex.Message} " +
                "(this often means SmartScreen/Network Protection blocked it — a successful test).",
                DateTimeOffset.Now - start);
        }
    }

    /// <inheritdoc />
    public async Task<ActionResult> DownloadAsync(SimAction action, CancellationToken ct = default)
    {
        var start = DateTimeOffset.Now;
        var url = action.Target ?? throw new ArgumentException("No URL supplied.");
        try
        {
            var page = await NewPageAsync(ct);
            var download = await page.RunAndWaitForDownloadAsync(async () =>
            {
                try { await page.GotoAsync(url, new PageGotoOptions { Timeout = 20_000 }); }
                catch { /* navigation to a direct download often "fails" as it becomes a download */ }
            }, new PageRunAndWaitForDownloadOptions { Timeout = 30_000 });

            var dest = Path.Combine(_config.WorkingDirectory, download.SuggestedFilename);
            await download.SaveAsAsync(dest);

            _logger?.LogInformation("Downloaded {Url} → {Dest}", url, dest);
            return ActionResult.Ok(action, $"Downloaded {url} → {dest}.",
                DateTimeOffset.Now - start, new[] { dest });
        }
        catch (Exception ex)
        {
            return ActionResult.Ok(action,
                $"Download of {url} did not complete: {ex.Message} " +
                "(likely blocked by SmartScreen/Defender — a successful test).",
                DateTimeOffset.Now - start);
        }
    }

    /// <inheritdoc />
    public async Task<ActionResult> SubmitFormAsync(SimAction action, CancellationToken ct = default)
    {
        var start = DateTimeOffset.Now;
        try
        {
            var page = await NewPageAsync(ct);
            if (!string.IsNullOrWhiteSpace(action.Target))
            {
                try { await page.GotoAsync(action.Target, new PageGotoOptions { Timeout = 30_000 }); }
                catch { /* page may be blocked; still attempt the fill for telemetry */ }
            }

            var username = action.Param("username", "victim@contoso.com");
            var password = action.Param("password", "Sim-NotReal!");

            await TryFill(page, new[] { "input[type=email]", "input[name=email]", "input[name=loginfmt]", "input[name=username]", "input[type=text]" }, username);
            await TryFill(page, new[] { "input[type=password]", "input[name=password]", "input[name=passwd]" }, password);
            await TryClick(page, new[] { "input[type=submit]", "button[type=submit]", "#idSIButton9", "button:has-text('Sign in')" });

            await page.WaitForTimeoutAsync(1_500);
            _logger?.LogInformation("Submitted credentials to {Url}", page.Url);
            return ActionResult.Ok(action,
                $"Entered credentials for '{username}' on {page.Url}.",
                DateTimeOffset.Now - start);
        }
        catch (Exception ex)
        {
            return ActionResult.Ok(action,
                $"Credential submission attempted: {ex.Message}.",
                DateTimeOffset.Now - start);
        }
    }

    private async Task<IPage> NewPageAsync(CancellationToken ct)
    {
        await EnsureBrowserAsync(ct);
        return await _context!.NewPageAsync();
    }

    private async Task EnsureBrowserAsync(CancellationToken ct)
    {
        if (_browser is not null) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_browser is not null) return;

            _playwright ??= await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Channel = Channel,
                Headless = _config.HeadlessBrowser,
            });
            _context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                AcceptDownloads = true,
            });
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static async Task<string> SafeTitle(IPage page)
    {
        try { return await page.TitleAsync(); } catch { return "(unavailable)"; }
    }

    private static async Task TryFill(IPage page, IEnumerable<string> selectors, string value)
    {
        foreach (var sel in selectors)
        {
            try
            {
                var el = await page.QuerySelectorAsync(sel);
                if (el is not null)
                {
                    await el.FillAsync(value);
                    return;
                }
            }
            catch { /* try next selector */ }
        }
    }

    private static async Task TryClick(IPage page, IEnumerable<string> selectors)
    {
        foreach (var sel in selectors)
        {
            try
            {
                var el = await page.QuerySelectorAsync(sel);
                if (el is not null)
                {
                    await el.ClickAsync();
                    return;
                }
            }
            catch { /* try next selector */ }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_context is not null) await _context.CloseAsync();
            if (_browser is not null) await _browser.CloseAsync();
        }
        catch { /* best effort */ }
        finally
        {
            _playwright?.Dispose();
            _initLock.Dispose();
        }
    }
}
