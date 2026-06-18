using System.ComponentModel;
using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using EvilEndUser.Core.Safety;
using EvilEndUser.MitreAttack;
using Microsoft.SemanticKernel;

namespace EvilEndUser.LLM.Plugins;

/// <summary>
/// A Semantic Kernel plugin that exposes the simulator's user actions as tools the LLM can invoke.
/// Each function builds a <see cref="SimAction"/> and runs it through the bound
/// <see cref="IScenarioContext"/>, so safety validation and telemetry always apply. The functions
/// return a short human-readable result string that is fed back to the model.
/// </summary>
public sealed class UserActionPlugin
{
    private readonly IScenarioContext _ctx;
    private CancellationToken _ct;

    /// <summary>Creates the plugin bound to a scenario context.</summary>
    public UserActionPlugin(IScenarioContext ctx) => _ctx = ctx;

    /// <summary>Sets the cancellation token used by tool invocations for the current run.</summary>
    public void SetCancellation(CancellationToken ct) => _ct = ct;

    [KernelFunction, Description("Open a web page in the browser by navigating to a URL.")]
    public async Task<string> BrowseUrl(
        [Description("The URL to visit. In SafeMode must be one of the approved test URLs.")] string url,
        [Description("A short in-character reason you are visiting it.")] string reason = "")
    {
        var action = Build(SimActionType.BrowseUrl, url, RiskLevel.Medium,
            TechniqueCatalog.DriveByCompromise, reason);
        return Describe(await _ctx.ExecuteAsync(action, _ct));
    }

    [KernelFunction, Description("Download a file from a URL via the browser.")]
    public async Task<string> DownloadFile(
        [Description("The URL of the file to download (approved test URL in SafeMode).")] string url,
        [Description("A short in-character reason.")] string reason = "")
    {
        var action = Build(SimActionType.DownloadFile, url, RiskLevel.High,
            TechniqueCatalog.UserExecutionLink, reason);
        return Describe(await _ctx.ExecuteAsync(action, _ct));
    }

    [KernelFunction, Description("Type your username and password into a login form on a web page (you are being phished).")]
    public async Task<string> SubmitLoginForm(
        [Description("The login page URL (approved phishing-demo URL in SafeMode).")] string url,
        [Description("The username/email you type in.")] string username,
        [Description("A short in-character reason you trusted the page.")] string reason = "")
    {
        var action = Build(SimActionType.SubmitWebForm, url, RiskLevel.High,
            TechniqueCatalog.PhishingForInfoLink, reason, new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = "Sim-NotARealPassword!" + _ctx.Random.Next(1000, 9999),
            });
        return Describe(await _ctx.ExecuteAsync(action, _ct));
    }

    [KernelFunction, Description("Save an attachment to disk by writing the EICAR antivirus test file, disguised with an invoice-like name.")]
    public async Task<string> WriteEicarFile(
        [Description("A deceptive file name, e.g. Invoice_2026.pdf.txt")] string fileName = "Invoice_2026.pdf.txt",
        [Description("A short in-character reason.")] string reason = "")
    {
        var action = Build(SimActionType.WriteFile, fileName, RiskLevel.High,
            TechniqueCatalog.UserExecutionFile, reason, new Dictionary<string, string>
            {
                ["content"] = TestPayloads.EicarString,
            });
        return Describe(await _ctx.ExecuteAsync(action, _ct));
    }

    [KernelFunction, Description("Open / double-click a file you previously saved.")]
    public async Task<string> OpenFile(
        [Description("The file name or path to open.")] string path,
        [Description("A short in-character reason.")] string reason = "")
    {
        var action = Build(SimActionType.OpenFile, path, RiskLevel.High,
            TechniqueCatalog.Masquerading, reason);
        return Describe(await _ctx.ExecuteAsync(action, _ct));
    }

    [KernelFunction, Description("Run a shell command, e.g. because a website told you to paste it to 'fix' an error.")]
    public async Task<string> RunCommand(
        [Description("The command line. In SafeMode must be one of the approved commands.")] string command,
        [Description("A short in-character reason.")] string reason = "")
    {
        var action = Build(SimActionType.RunProcess, command, RiskLevel.High,
            TechniqueCatalog.PowerShell, reason);
        return Describe(await _ctx.ExecuteAsync(action, _ct));
    }

    [KernelFunction, Description("Send an email, optionally to an external address (e.g. forwarding work files to your personal account).")]
    public async Task<string> SendEmail(
        [Description("Recipient email address.")] string to,
        [Description("Email subject.")] string subject,
        [Description("Email body.")] string body,
        [Description("A short in-character reason.")] string reason = "")
    {
        var action = Build(SimActionType.SendEmail, to, RiskLevel.High,
            TechniqueCatalog.SelfExfiltration, reason, new Dictionary<string, string>
            {
                ["subject"] = subject,
                ["body"] = body,
                ["external"] = "true",
            });
        return Describe(await _ctx.ExecuteAsync(action, _ct));
    }

    [KernelFunction, Description("Send a Microsoft Teams chat message to a colleague, optionally containing a link.")]
    public async Task<string> SendTeamsMessage(
        [Description("Recipient UPN/email.")] string to,
        [Description("The message text (may include a link).")] string message,
        [Description("A short in-character reason.")] string reason = "")
    {
        var action = Build(SimActionType.SendTeamsMessage, to, RiskLevel.Medium,
            TechniqueCatalog.InternalSpearphishing, reason, new Dictionary<string, string>
            {
                ["message"] = message,
            });
        return Describe(await _ctx.ExecuteAsync(action, _ct));
    }

    [KernelFunction, Description("Do nothing for a moment (think / hesitate).")]
    public async Task<string> Wait(
        [Description("A short in-character reason for pausing.")] string reason = "")
    {
        await _ctx.ThinkAsync(_ct);
        _ctx.Log($"User paused: {reason}");
        return "You waited for a moment.";
    }

    [KernelFunction, Description("Open one of your desktop apps (outlook, teams, edge, chrome, explorer, notepad, word, excel).")]
    public async Task<string> OpenDesktopApp(
        [Description("Friendly app name: outlook, teams, edge, chrome, explorer, notepad, word, excel.")] string app,
        [Description("A short in-character reason.")] string reason = "")
    {
        var action = Build(SimActionType.LaunchDesktopApp, app, RiskLevel.Safe,
            TechniqueCatalog.UserExecutionFile, reason);
        return Describe(await _ctx.ExecuteAsync(action, _ct));
    }

    private SimAction Build(
        SimActionType type, string target, RiskLevel risk, MitreTechnique technique,
        string? reason, IReadOnlyDictionary<string, string>? parameters = null)
        => new()
        {
            Type = type,
            Target = target,
            RiskLevel = risk,
            Technique = technique,
            Rationale = string.IsNullOrWhiteSpace(reason) ? null : reason,
            Parameters = parameters ?? new Dictionary<string, string>(),
        };

    private static string Describe(ActionResult result)
        => result.Success
            ? $"OK: {result.Message}"
            : $"BLOCKED/FAILED: {result.Message} {result.Error}".Trim();
}
