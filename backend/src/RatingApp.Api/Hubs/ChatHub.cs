using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RatingApp.Application.DTOs.Chats;
using RatingApp.Application.Services;
using System.Security.Claims;

namespace RatingApp.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ChatService chatService, ILogger<ChatHub> logger)
        => (_chatService, _logger) = (chatService, logger);

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            var chats = await _chatService.GetUserChatsAsync(userId);

            foreach (var chat in chats)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{chat.ChatId}");

            _logger.LogInformation("SignalR: User {UserId} connected, joined {Count} chat groups",
                userId, chats.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR: User disconnected. ConnectionId: {Id}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client sends a message via the hub. Server persists it and broadcasts to the chat group.
    /// </summary>
    public async Task SendMessage(Guid chatId, string content)
    {
        var userId = GetCurrentUserId();

        try
        {
            var message = await _chatService.SendMessageAsync(
                chatId, userId, new SendMessageRequest(content));

            await Clients.Group($"chat-{chatId}")
                .SendAsync("ReceiveMessage", message);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    /// <summary>
    /// Client calls this after a new match is created to join the new chat group.
    /// </summary>
    public async Task JoinChat(Guid chatId)
    {
        var userId = GetCurrentUserId();

        try
        {
            await _chatService.EnsureAccessAsync(chatId, userId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{chatId}");
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    private Guid GetCurrentUserId()
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? Context.User?.FindFirstValue("sub")
               ?? throw new HubException("User not authenticated.");
        return Guid.Parse(sub);
    }
}
