using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RatingApp.Application.DTOs.Ratings;
using RatingApp.Application.Services;
using System.Security.Claims;

namespace RatingApp.Api.Controllers;

[ApiController]
[Route("api/ratings")]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;
    private readonly IValidator<RateUserRequest> _validator;

    public RatingsController(IRatingService ratingService, IValidator<RateUserRequest> validator)
        => (_ratingService, _validator) = (ratingService, validator);

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim not found."));

    [HttpPost]
    public async Task<IActionResult> SubmitRating([FromBody] RateUserRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            var (matchCreated, matchId) = await _ratingService.SubmitRatingAsync(CurrentUserId, request);
            return Ok(new { matchCreated, matchId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("summary/{userId:guid}")]
    public async Task<IActionResult> GetRatingSummary(Guid userId)
    {
        var summary = await _ratingService.GetRatingSummaryAsync(userId);
        return Ok(summary);
    }
}
