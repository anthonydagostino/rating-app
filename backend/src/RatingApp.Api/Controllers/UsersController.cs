using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RatingApp.Application.DTOs.Users;
using RatingApp.Application.Services;
using System.Security.Claims;

namespace RatingApp.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly RatingService _ratingService;

    public UsersController(UserService userService, RatingService ratingService)
        => (_userService, _ratingService) = (userService, ratingService);

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim not found."));

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile() =>
        Ok(await _userService.GetProfileAsync(CurrentUserId));

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req) =>
        Ok(await _userService.UpdateProfileAsync(CurrentUserId, req));

    [HttpGet("me/preferences")]
    public async Task<IActionResult> GetPreferences() =>
        Ok(await _userService.GetPreferencesAsync(CurrentUserId));

    [HttpPut("me/preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] PreferenceDto req) =>
        Ok(await _userService.UpsertPreferencesAsync(CurrentUserId, req));

    [HttpGet("me/rating-summary")]
    public async Task<IActionResult> GetRatingSummary() =>
        Ok(await _ratingService.GetRatingSummaryAsync(CurrentUserId));
}
