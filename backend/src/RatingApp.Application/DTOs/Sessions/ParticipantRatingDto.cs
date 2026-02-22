namespace RatingApp.Application.DTOs.Sessions;

public record ParticipantRatingDto(
    Guid RaterUserId,
    string RaterDisplayName,
    int? Score,
    string? Notes,
    DateTime UpdatedAt);
