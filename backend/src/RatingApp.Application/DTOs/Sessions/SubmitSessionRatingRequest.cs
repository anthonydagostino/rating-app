namespace RatingApp.Application.DTOs.Sessions;

public record SubmitSessionRatingRequest(int Score, string? Notes = null);