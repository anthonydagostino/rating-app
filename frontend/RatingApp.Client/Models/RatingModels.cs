namespace RatingApp.Client.Models;

public record CriterionModel(Guid Id, string Name, double Weight, bool IsRequired);

public record CriterionScoreModel(Guid CriterionId, int Score);

public record MultiCriteriaRatingRequest(Guid RatedUserId, List<CriterionScoreModel> Criteria, string? Comment);

public record RatingDetailModel(Guid CriterionId, string CriterionName, double AverageScore, double Weight);

public record AggregateRatingModel(List<RatingDetailModel> CriteriaAverages, double WeightedAggregate, int TotalRatings);