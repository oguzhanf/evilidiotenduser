using EvilEndUser.Core.Models;

namespace EvilEndUser.Core.Abstractions;

/// <summary>
/// Marker base interface for all automation drivers that can carry out <see cref="SimAction"/>s
/// against a real desktop application or OS surface.
/// </summary>
public interface IAutomationDriver : IAsyncDisposable
{
    /// <summary>Short, stable name used in telemetry and the UI (e.g. "Edge", "Outlook").</summary>
    string Name { get; }

    /// <summary>
    /// Returns true when the underlying application/tooling is present and usable on this endpoint.
    /// Drivers should never throw from this probe.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}

/// <summary>
/// Drives a real web browser (Edge or Chrome) the way an end user would: opening URLs, downloading
/// files, and typing into login forms. Backed by Playwright in the Automation project.
/// </summary>
public interface IBrowserDriver : IAutomationDriver
{
    /// <summary>Navigates to a URL in a visible browser tab.</summary>
    Task<ActionResult> NavigateAsync(SimAction action, CancellationToken ct = default);

    /// <summary>Triggers a file download from a URL and waits for it to land on disk.</summary>
    Task<ActionResult> DownloadAsync(SimAction action, CancellationToken ct = default);

    /// <summary>
    /// Fills and submits a login form on the current/target page (credential-phishing emulation).
    /// Expects parameters "username" and "password"; "url" optionally navigates first.
    /// </summary>
    Task<ActionResult> SubmitFormAsync(SimAction action, CancellationToken ct = default);
}

/// <summary>
/// Drives email: reading the inbox for "phishing" links, clicking them, opening attachments, and
/// sending mail. Implemented over Microsoft Graph and/or Outlook COM interop.
/// </summary>
public interface IEmailDriver : IAutomationDriver
{
    /// <summary>Sends an email (optionally to an external recipient, optionally with attachments).</summary>
    Task<ActionResult> SendAsync(SimAction action, CancellationToken ct = default);

    /// <summary>Opens an attachment that arrived in the inbox (double-click emulation).</summary>
    Task<ActionResult> OpenAttachmentAsync(SimAction action, CancellationToken ct = default);

    /// <summary>Reads the most recent inbox messages so a scenario/brain can react to them.</summary>
    Task<IReadOnlyList<MailSummary>> ReadInboxAsync(int maxCount = 20, CancellationToken ct = default);
}

/// <summary>A lightweight projection of an inbox message.</summary>
public sealed record MailSummary(string Subject, string Sender, DateTimeOffset Received, IReadOnlyList<string> Links);

/// <summary>
/// Drives Microsoft Teams: sending chat/channel messages, optionally containing risky links or
/// files (internal spearphishing / lateral-movement emulation). Implemented over Microsoft Graph.
/// </summary>
public interface ITeamsDriver : IAutomationDriver
{
    /// <summary>Sends a Teams chat or channel message.</summary>
    Task<ActionResult> SendMessageAsync(SimAction action, CancellationToken ct = default);
}

/// <summary>
/// Runs processes / shell commands the way a user tricked by a "ClickFix" lure would.
/// </summary>
public interface IProcessDriver : IAutomationDriver
{
    /// <summary>Executes a command line and captures its output.</summary>
    Task<ActionResult> RunAsync(SimAction action, CancellationToken ct = default);

    /// <summary>Writes/updates a value under the current-user registry hive.</summary>
    Task<ActionResult> ModifyRegistryAsync(SimAction action, CancellationToken ct = default);
}

/// <summary>
/// Performs filesystem actions: dropping test payloads, opening files, and archiving data for
/// exfiltration emulation.
/// </summary>
public interface IFileSystemDriver : IAutomationDriver
{
    /// <summary>Writes content (e.g. the EICAR string) to a file, returning its path.</summary>
    Task<ActionResult> WriteFileAsync(SimAction action, CancellationToken ct = default);

    /// <summary>Opens / launches a file using its default handler.</summary>
    Task<ActionResult> OpenFileAsync(SimAction action, CancellationToken ct = default);
}

/// <summary>
/// Launches and focuses the <i>real</i> desktop applications a user interacts with (Outlook, Teams,
/// Edge, File Explorer, …). Launching the genuine processes generates authentic process-creation
/// telemetry for Defender for Endpoint, exactly as if the user opened the apps themselves.
/// </summary>
public interface IDesktopDriver : IAutomationDriver
{
    /// <summary>
    /// Launches a desktop app identified by a friendly name in <see cref="SimAction.Target"/>
    /// (e.g. "outlook", "teams", "edge", "explorer", "notepad") and brings its window to the front.
    /// </summary>
    Task<ActionResult> LaunchAppAsync(SimAction action, CancellationToken ct = default);
}
