namespace RatingApp.Application.DTOs.Ratings;

public record RateUserRequest(Guid RatedUserId, int Score, string? Comment = null);