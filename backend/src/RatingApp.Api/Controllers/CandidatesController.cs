using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RatingApp.Application.Services;
using System.Security.Claims;

namespace RatingApp.Api.Controllers;

[ApiController]
[Route("api/candidates")]
[Authorize]
public class CandidatesController : ControllerBase
{
    private readonly CandidateService _candidateService;

    public CandidatesController(CandidateService candidateService)
        => _candidateService = candidateService;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim not found."));

    [HttpGet]
    public async Task<IActionResult> GetFeed([FromQuery] int pageSize = 10)
    {
        if (pageSize < 1 || pageSize > 50)
            return BadRequest(new { message = "pageSize must be between 1 and 50." });

        var candidates = await _candidateService.GetCandidatesAsync(CurrentUserId, pageSize);
        return Ok(candidates);
    }
}
