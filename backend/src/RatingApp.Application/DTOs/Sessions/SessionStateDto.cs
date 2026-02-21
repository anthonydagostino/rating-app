namespace RatingApp.Application.DTOs.Sessions;

public record SessionStateDto(
    Guid SessionId,
    string Status,
    List<ParticipantRatingDto> ParticipantRatings,
    List<SessionMessageDto> RecentMessages);