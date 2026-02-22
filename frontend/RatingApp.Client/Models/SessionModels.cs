namespace RatingApp.Client.Models;

public record CreateSessionRequest(Guid CandidateId, string? Title);

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

public record ParticipantRatingDto(
    Guid RaterUserId,
    string RaterDisplayName,
    int? Score,
    string? Notes,
    DateTime? UpdatedAt);

public record SubmitSessionRatingRequest(int Score, string? Notes);

public record UpdateSessionRatingRequest(int Score, string? Notes);

public record SessionStateDto(
    Guid SessionId,
    string Status,
    List<ParticipantRatingDto> ParticipantRatings,
    List<SessionChatMessageDto> RecentMessages);

public record SessionChatMessageDto(
    Guid Id,
    Guid SenderUserId,
    string SenderDisplayName,
    string Content,
    DateTime SentAt);