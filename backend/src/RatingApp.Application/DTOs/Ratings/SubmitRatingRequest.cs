namespace RatingApp.Application.DTOs.Ratings;

public record SubmitRatingRequest(Guid RatedUserId, int Score);
