using RatingApp.Application.DTOs.Ratings;

namespace RatingApp.Application.Services;

public interface IRatingService
{
    Task<(bool MatchCreated, Guid? MatchId)> SubmitRatingAsync(Guid raterId, RateUserRequest request);
    Task<RatingSummaryDto> GetRatingSummaryAsync(Guid userId);
}
