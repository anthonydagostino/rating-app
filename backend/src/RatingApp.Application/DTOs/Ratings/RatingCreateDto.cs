namespace RatingApp.Application.DTOs.Ratings;

public record RatingCriterionScoreDto(Guid CriterionId, int Score);

public record RatingCreateDto(Guid RatedUserId, List<RatingCriterionScoreDto> Criteria, string? Comment);