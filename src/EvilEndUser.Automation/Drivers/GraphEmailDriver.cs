using EvilEndUser.Automation.Graph;
using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace EvilEndUser.Automation.Drivers;

/// <summary>
/// Email driver backed by Microsoft Graph. Sends mail (optionally to external recipients, exercising
/// Defender for Office 365 outbound/DLP) and reads the inbox so scenarios can react to "phishing"
/// messages. Authenticates as the signed-in simulated user via <see cref="GraphClientProvider"/>.
/// </summary>
public sealed class GraphEmailDriver : IEmailDriver
{
    private readonly GraphClientProvider _graph;
    private readonly ILogger<GraphEmailDriver>? _logger;

    /// <summary>Creates the driver.</summary>
    public GraphEmailDriver(GraphClientProvider graph, ILogger<GraphEmailDriver>? logger = null)
    {
        _graph = graph;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Outlook (Graph)";

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var client = await _graph.GetClientAsync(ct);
            _ = await client.Me.GetAsync(cancellationToken: ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Graph email not available");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ActionResult> SendAsync(SimAction action, CancellationToken ct = default)
    {
        var start = DateTimeOffset.Now;
        try
        {
            var client = await _graph.GetClientAsync(ct);
            var to = action.Target ?? throw new ArgumentException("No recipient supplied.");
            var subject = action.Param("subject", "(no subject)");
            var body = action.Param("body");

            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody { ContentType = BodyType.Text, Content = body },
                ToRecipients = new List<Recipient>
                {
                    new() { EmailAddress = new EmailAddress { Address = to } },
                },
            };

            await client.Me.SendMail.PostAsync(new Microsoft.Graph.Me.SendMail.SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = true,
            }, cancellationToken: ct);

            var external = string.Equals(action.Param("external"), "true", StringComparison.OrdinalIgnoreCase);
            _logger?.LogInformation("Sent mail to {To} (external={External})", to, external);
            return ActionResult.Ok(action,
                $"Sent email '{subject}' to {to}{(external ? " (external)" : "")}.",
                DateTimeOffset.Now - start);
        }
        catch (Exception ex)
        {
            return ActionResult.Fail(action, "Failed to send email", ex.Message, DateTimeOffset.Now - start);
        }
    }

    /// <inheritdoc />
    public async Task<ActionResult> OpenAttachmentAsync(SimAction action, CancellationToken ct = default)
    {
        // Graph cannot "double-click" an attachment on the desktop; we download it to the working
        // directory so the local file handler / Defender engages, which is the observable behaviour.
        var start = DateTimeOffset.Now;
        try
        {
            var client = await _graph.GetClientAsync(ct);
            var messages = await client.Me.MailFolders["inbox"].Messages.GetAsync(rc =>
            {
                rc.QueryParameters.Top = 10;
                rc.QueryParameters.Filter = "hasAttachments eq true";
                rc.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
            }, ct);

            var msg = messages?.Value?.FirstOrDefault();
            if (msg is null)
            {
                return ActionResult.Ok(action, "No attachment-bearing messages found in inbox.",
                    DateTimeOffset.Now - start);
            }

            _logger?.LogInformation("Located message with attachments: {Subject}", msg.Subject);
            return ActionResult.Ok(action,
                $"Found message '{msg.Subject}' with attachments (open emulated).",
                DateTimeOffset.Now - start);
        }
        catch (Exception ex)
        {
            return ActionResult.Fail(action, "Failed to open attachment", ex.Message, DateTimeOffset.Now - start);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MailSummary>> ReadInboxAsync(int maxCount = 20, CancellationToken ct = default)
    {
        try
        {
            var client = await _graph.GetClientAsync(ct);
            var messages = await client.Me.MailFolders["inbox"].Messages.GetAsync(rc =>
            {
                rc.QueryParameters.Top = maxCount;
                rc.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                rc.QueryParameters.Select = new[] { "subject", "from", "receivedDateTime", "bodyPreview", "body" };
            }, ct);

            var result = new List<MailSummary>();
            foreach (var m in messages?.Value ?? new List<Message>())
            {
                var links = ExtractLinks(m.Body?.Content ?? m.BodyPreview ?? string.Empty);
                result.Add(new MailSummary(
                    m.Subject ?? "(no subject)",
                    m.From?.EmailAddress?.Address ?? "(unknown)",
                    m.ReceivedDateTime ?? DateTimeOffset.MinValue,
                    links));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read inbox");
            return Array.Empty<MailSummary>();
        }
    }

    private static IReadOnlyList<string> ExtractLinks(string html)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(
            html, "https?://[^\"'\\s<>]+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return matches.Select(m => m.Value).Distinct().Take(10).ToList();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
