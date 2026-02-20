namespace RatingApp.Application.DTOs.Chats;

public record ChatSummaryDto(
    Guid ChatId,
    Guid MatchId,
    Guid OtherUserId,
    string OtherUserDisplayName,
    string? LastMessageSnippet,
    DateTime? LastMessageAt,
    DateTime MatchedAt
);
