using RatingApp.Application.DTOs.Ratings;

namespace RatingApp.Application.Services;

public interface IRatingService
{
    Task<Guid?> AddRating(Guid raterId, RatingCreateDto dto);
    Task<AggregateRatingDto> GetAggregatedScores(Guid candidateId);
    Task<List<CriterionDto>> GetCriteriaAsync();
    Task<RatingSummaryDto> GetRatingSummaryAsync(Guid userId);
}