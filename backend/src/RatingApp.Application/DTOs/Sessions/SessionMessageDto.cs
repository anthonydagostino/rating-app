namespace RatingApp.Application.DTOs.Sessions;

public record SessionMessageDto(
    Guid Id,
    Guid SenderUserId,
    string SenderDisplayName,
    string Content,
    DateTime SentAt);