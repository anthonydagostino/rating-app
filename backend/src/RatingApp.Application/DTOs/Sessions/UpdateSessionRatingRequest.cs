namespace RatingApp.Application.DTOs.Sessions;

public record UpdateSessionRatingRequest(int Score, string? Notes = null);
