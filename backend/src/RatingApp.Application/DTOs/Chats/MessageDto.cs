namespace RatingApp.Application.DTOs.Chats;

public record MessageDto(Guid Id, Guid ChatId, Guid SenderUserId, string Content, DateTime CreatedAt);
