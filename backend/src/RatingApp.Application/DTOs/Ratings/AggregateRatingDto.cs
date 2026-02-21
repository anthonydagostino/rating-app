namespace RatingApp.Application.DTOs.Ratings;

public record RatingDetailDto(Guid CriterionId, string CriterionName, double AverageScore, double Weight);

public record AggregateRatingDto(List<RatingDetailDto> CriteriaAverages, double WeightedAggregate, int TotalRatings);