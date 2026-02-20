using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RatingApp.Application.DTOs.Chats;
using RatingApp.Application.Services;
using RatingApp.Api.Hubs;
using System.Security.Claims;

namespace RatingApp.Api.Controllers;

[ApiController]
[Route("api/chats")]
[Authorize]
public class ChatsController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatsController(ChatService chatService, IHubContext<ChatHub> hubContext)
        => (_chatService, _hubContext) = (chatService, hubContext);

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim not found."));

    [HttpGet]
    public async Task<IActionResult> GetChats() =>
        Ok(await _chatService.GetUserChatsAsync(CurrentUserId));

    [HttpGet("{chatId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid chatId)
    {
        try
        {
            return Ok(await _chatService.GetMessagesAsync(chatId, CurrentUserId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPost("{chatId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid chatId, [FromBody] SendMessageRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(new { message = "Message content cannot be empty." });

        try
        {
            var message = await _chatService.SendMessageAsync(chatId, CurrentUserId, req);

            // Broadcast via SignalR
            await _hubContext.Clients.Group($"chat-{chatId}")
                .SendAsync("ReceiveMessage", message);

            return Ok(message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
