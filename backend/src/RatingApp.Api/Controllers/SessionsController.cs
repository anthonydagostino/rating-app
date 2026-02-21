using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RatingApp.Application.DTOs.Sessions;
using RatingApp.Application.Services;
using System.Security.Claims;

namespace RatingApp.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly RatingSessionService _sessionService;

    public SessionsController(RatingSessionService sessionService)
        => _sessionService = sessionService;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim not found."));

    /// <summary>POST /api/sessions — Create a new rating session for a candidate.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
    {
        try
        {
            var session = await _sessionService.CreateSessionAsync(CurrentUserId, request);
            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>GET /api/sessions/{id} — Get session details (participants must be a member).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id)
    {
        try
        {
            var session = await _sessionService.GetSessionAsync(id, CurrentUserId);
            return Ok(session);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
    }

    /// <summary>POST /api/sessions/{id}/lock — Lock the session (creator only).</summary>
    [HttpPost("{id:guid}/lock")]
    public async Task<IActionResult> LockSession(Guid id)
    {
        try
        {
            await _sessionService.LockSessionAsync(id, CurrentUserId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>POST /api/sessions/{id}/finalize — Finalize and persist aggregate rating (creator only).</summary>
    [HttpPost("{id:guid}/finalize")]
    public async Task<IActionResult> FinalizeSession(Guid id)
    {
        try
        {
            await _sessionService.FinalizeSessionAsync(id, CurrentUserId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
