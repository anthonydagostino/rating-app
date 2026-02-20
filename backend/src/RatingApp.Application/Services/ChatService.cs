using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RatingApp.Application.DTOs.Chats;
using RatingApp.Domain.Entities;
using RatingApp.Infrastructure.Persistence;

namespace RatingApp.Application.Services;

public class ChatService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ChatService> _logger;

    public ChatService(AppDbContext db, ILogger<ChatService> logger)
        => (_db, _logger) = (db, logger);

    public async Task<List<ChatSummaryDto>> GetUserChatsAsync(Guid userId)
    {
        var matches = await _db.Matches
            .Include(m => m.Chat)
                .ThenInclude(c => c!.Messages.OrderByDescending(msg => msg.CreatedAt).Take(1))
            .Include(m => m.UserA)
            .Include(m => m.UserB)
            .Where(m => (m.UserAId == userId || m.UserBId == userId) && m.Chat != null)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return matches.Select(m =>
        {
            var other = m.UserAId == userId ? m.UserB : m.UserA;
            var lastMsg = m.Chat!.Messages.MaxBy(msg => msg.CreatedAt);
            return new ChatSummaryDto(
                m.Chat!.Id,
                m.Id,
                other.Id,
                other.DisplayName,
                lastMsg?.Content.Length > 50 ? lastMsg.Content[..50] + "..." : lastMsg?.Content,
                lastMsg?.CreatedAt,
                m.CreatedAt);
        }).ToList();
    }

    public async Task<List<MessageDto>> GetMessagesAsync(Guid chatId, Guid requestingUserId)
    {
        await EnsureAccessAsync(chatId, requestingUserId);

        return await _db.Messages
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .Select(m => new MessageDto(m.Id, m.ChatId, m.SenderUserId, m.Content, m.CreatedAt))
            .ToListAsync();
    }

    public async Task<MessageDto> SendMessageAsync(Guid chatId, Guid senderId, SendMessageRequest req)
    {
        await EnsureAccessAsync(chatId, senderId);

        if (string.IsNullOrWhiteSpace(req.Content))
            throw new ArgumentException("Message content cannot be empty.");

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            SenderUserId = senderId,
            Content = req.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Message sent in chat {ChatId} by {SenderId}", chatId, senderId);
        return new MessageDto(message.Id, chatId, senderId, message.Content, message.CreatedAt);
    }

    public async Task EnsureAccessAsync(Guid chatId, Guid userId)
    {
        var chat = await _db.Chats
            .Include(c => c.Match)
            .FirstOrDefaultAsync(c => c.Id == chatId)
            ?? throw new KeyNotFoundException("Chat not found.");

        bool isParticipant = chat.Match.UserAId == userId || chat.Match.UserBId == userId;
        if (!isParticipant)
            throw new UnauthorizedAccessException("You are not a participant in this chat.");
    }
}
