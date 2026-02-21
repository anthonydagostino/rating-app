namespace RatingApp.Application.DTOs.Sessions;

public record SessionDto(
    Guid Id,
    Guid CandidateId,
    string CandidateDisplayName,
    Guid CreatorId,
    string CreatorDisplayName,
    string? Title,
    string Status,
    DateTime CreatedAt,
    DateTime? FinalizedAt,
    List<ParticipantRatingDto> ParticipantRatings);