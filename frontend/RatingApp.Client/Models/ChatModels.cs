namespace RatingApp.Client.Models;

public record ChatSummaryDto(
    Guid ChatId,
    Guid MatchId,
    Guid OtherUserId,
    string OtherUserDisplayName,
    string? LastMessageSnippet,
    DateTime? LastMessageAt,
    DateTime MatchedAt
);

public record MessageDto(Guid Id, Guid ChatId, Guid SenderUserId, string Content, DateTime CreatedAt);

public record SendMessageRequest(string Content);
