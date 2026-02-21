using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RatingApp.Application.DTOs.Sessions;
using RatingApp.Application.Services;
using System.Security.Claims;

namespace RatingApp.Api.Hubs;

[Authorize]
public class RatingHub : Hub
{
    private readonly RatingSessionService _sessionService;
    private readonly ILogger<RatingHub> _logger;

    public RatingHub(RatingSessionService sessionService, ILogger<RatingHub> logger)
        => (_sessionService, _logger) = (sessionService, logger);

    /// <summary>
    /// Join a session group and notify other participants.
    /// </summary>
    public async Task JoinSession(Guid sessionId)
    {
        var userId = GetCurrentUserId();

        try
        {
            await _sessionService.JoinSessionAsync(sessionId, userId);
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupKey(sessionId));
            await Clients.Group(GroupKey(sessionId))
                .SendAsync("UserJoined", new { userId, sessionId });

            _logger.LogInformation("User {UserId} joined session {SessionId}", userId, sessionId);
        }
        catch (KeyNotFoundException ex) { throw new HubException(ex.Message); }
        catch (InvalidOperationException ex) { throw new HubException(ex.Message); }
    }

    /// <summary>
    /// Leave a session group and notify other participants.
    /// </summary>
    public async Task LeaveSession(Guid sessionId)
    {
        var userId = GetCurrentUserId();

        try
        {
            await _sessionService.LeaveSessionAsync(sessionId, userId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupKey(sessionId));
            await Clients.Group(GroupKey(sessionId))
                .SendAsync("UserLeft", new { userId, sessionId });
        }
        catch (KeyNotFoundException ex) { throw new HubException(ex.Message); }
    }

    /// <summary>
    /// Submit a rating for the candidate and broadcast to all session participants.
    /// </summary>
    public async Task SubmitRating(Guid sessionId, SubmitSessionRatingRequest ratingDto)
    {
        var userId = GetCurrentUserId();

        try
        {
            var result = await _sessionService.SubmitRatingAsync(sessionId, userId, ratingDto);
            await Clients.Group(GroupKey(sessionId))
                .SendAsync("RatingSubmitted", result);
        }
        catch (KeyNotFoundException ex) { throw new HubException(ex.Message); }
        catch (UnauthorizedAccessException ex) { throw new HubException(ex.Message); }
        catch (InvalidOperationException ex) { throw new HubException(ex.Message); }
        catch (ArgumentException ex) { throw new HubException(ex.Message); }
    }

    /// <summary>
    /// Update an existing rating and broadcast the change.
    /// </summary>
    public async Task UpdateRating(Guid sessionId, UpdateSessionRatingRequest ratingUpdateDto)
    {
        var userId = GetCurrentUserId();

        try
        {
            var result = await _sessionService.UpdateRatingAsync(sessionId, userId, ratingUpdateDto);
            await Clients.Group(GroupKey(sessionId))
                .SendAsync("RatingUpdated", result);
        }
        catch (KeyNotFoundException ex) { throw new HubException(ex.Message); }
        catch (UnauthorizedAccessException ex) { throw new HubException(ex.Message); }
        catch (InvalidOperationException ex) { throw new HubException(ex.Message); }
        catch (ArgumentException ex) { throw new HubException(ex.Message); }
    }

    /// <summary>
    /// Send a chat message within the session.
    /// </summary>
    public async Task SendChatMessage(Guid sessionId, string content)
    {
        var userId = GetCurrentUserId();

        try
        {
            var message = await _sessionService.SendMessageAsync(sessionId, userId, content);
            await Clients.Group(GroupKey(sessionId))
                .SendAsync("ChatMessage", message);
        }
        catch (KeyNotFoundException ex) { throw new HubException(ex.Message); }
        catch (UnauthorizedAccessException ex) { throw new HubException(ex.Message); }
        catch (InvalidOperationException ex) { throw new HubException(ex.Message); }
    }

    /// <summary>
    /// Returns the full current state of a session (ratings + recent chat).
    /// </summary>
    public async Task<SessionStateDto> SessionState(Guid sessionId)
    {
        var userId = GetCurrentUserId();

        try
        {
            return await _sessionService.GetSessionStateAsync(sessionId, userId);
        }
        catch (KeyNotFoundException ex) { throw new HubException(ex.Message); }
        catch (UnauthorizedAccessException ex) { throw new HubException(ex.Message); }
    }

    private Guid GetCurrentUserId()
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? Context.User?.FindFirstValue("sub")
               ?? throw new HubException("User not authenticated.");
        return Guid.Parse(sub);
    }

    private static string GroupKey(Guid sessionId) => $"session-{sessionId}";
}