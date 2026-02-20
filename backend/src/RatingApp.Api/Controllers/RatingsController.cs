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
    private readonly RatingService _ratingService;
    private readonly IValidator<SubmitRatingRequest> _validator;

    public RatingsController(RatingService ratingService, IValidator<SubmitRatingRequest> validator)
        => (_ratingService, _validator) = (ratingService, validator);

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim not found."));

    [HttpPost]
    public async Task<IActionResult> SubmitRating([FromBody] SubmitRatingRequest req)
    {
        var validation = await _validator.ValidateAsync(req);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            var matchId = await _ratingService.SubmitRatingAsync(CurrentUserId, req);
            return Ok(new { matchCreated = matchId.HasValue, matchId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
