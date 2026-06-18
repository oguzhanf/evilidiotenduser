using EvilEndUser.Automation.Graph;
using EvilEndUser.Core.Abstractions;
using EvilEndUser.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace EvilEndUser.Automation.Drivers;

/// <summary>
/// Microsoft Teams driver backed by Microsoft Graph. Sends a chat message to a recipient (resolving
/// or creating a one-on-one chat), optionally containing a link — exercising Safe Links for Microsoft
/// Teams and internal-spearphishing detections.
/// </summary>
public sealed class GraphTeamsDriver : ITeamsDriver
{
    private readonly GraphClientProvider _graph;
    private readonly ILogger<GraphTeamsDriver>? _logger;

    /// <summary>Creates the driver.</summary>
    public GraphTeamsDriver(GraphClientProvider graph, ILogger<GraphTeamsDriver>? logger = null)
    {
        _graph = graph;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Teams (Graph)";

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
            _logger?.LogWarning(ex, "Graph Teams not available");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ActionResult> SendMessageAsync(SimAction action, CancellationToken ct = default)
    {
        var start = DateTimeOffset.Now;
        try
        {
            var client = await _graph.GetClientAsync(ct);
            var recipientUpn = action.Target ?? throw new ArgumentException("No recipient supplied.");
            var text = action.Param("message", "Hello from the simulator.");

            // Resolve the recipient and (the signed-in) sender to build a 1:1 chat.
            var me = await client.Me.GetAsync(cancellationToken: ct);
            var recipient = await client.Users[recipientUpn].GetAsync(cancellationToken: ct);

            if (me?.Id is null || recipient?.Id is null)
            {
                return ActionResult.Fail(action, "Could not resolve sender/recipient user IDs.");
            }

            var chat = new Chat
            {
                ChatType = ChatType.OneOnOne,
                Members = new List<ConversationMember>
                {
                    new AadUserConversationMember
                    {
                        OdataType = "#microsoft.graph.aadUserConversationMember",
                        Roles = new List<string> { "owner" },
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["user@odata.bind"] = $"https://graph.microsoft.com/v1.0/users('{me.Id}')",
                        },
                    },
                    new AadUserConversationMember
                    {
                        OdataType = "#microsoft.graph.aadUserConversationMember",
                        Roles = new List<string> { "owner" },
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["user@odata.bind"] = $"https://graph.microsoft.com/v1.0/users('{recipient.Id}')",
                        },
                    },
                },
            };

            var createdChat = await client.Chats.PostAsync(chat, cancellationToken: ct);
            if (createdChat?.Id is null)
            {
                return ActionResult.Fail(action, "Failed to create Teams chat.");
            }

            var message = new ChatMessage
            {
                Body = new ItemBody { ContentType = BodyType.Html, Content = text },
            };
            await client.Chats[createdChat.Id].Messages.PostAsync(message, cancellationToken: ct);

            _logger?.LogInformation("Sent Teams message to {Upn}", recipientUpn);
            return ActionResult.Ok(action, $"Sent Teams message to {recipientUpn}.",
                DateTimeOffset.Now - start);
        }
        catch (Exception ex)
        {
            return ActionResult.Fail(action, "Failed to send Teams message", ex.Message,
                DateTimeOffset.Now - start);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
