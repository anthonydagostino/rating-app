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
    private readonly IValidator<RatingCreateDto> _validator;

    public RatingsController(IRatingService ratingService, IValidator<RatingCreateDto> validator)
        => (_ratingService, _validator) = (ratingService, validator);

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim not found."));

    [HttpGet("criteria")]
    public async Task<IActionResult> GetCriteria()
    {
        var criteria = await _ratingService.GetCriteriaAsync();
        return Ok(criteria);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitRating([FromBody] RatingCreateDto req)
    {
        var validation = await _validator.ValidateAsync(req);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            var matchId = await _ratingService.AddRating(CurrentUserId, req);
            return Ok(new { matchCreated = matchId.HasValue, matchId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("candidates/{id:guid}/aggregate")]
    public async Task<IActionResult> GetAggregate(Guid id)
    {
        var aggregate = await _ratingService.GetAggregatedScores(id);
        return Ok(aggregate);
    }
}