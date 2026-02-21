using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RatingApp.Application.DTOs.Chats;
using RatingApp.Application.Services;
using RatingApp.Application.Tests.Helpers;
using RatingApp.Domain.Entities;
using RatingApp.Domain.Enums;
using RatingApp.Infrastructure.Persistence;

namespace RatingApp.Application.Tests.Services;

public class ChatServiceTests
{
    private static (ChatService svc, AppDbContext db) CreateService()
    {
        var db = InMemoryDbFactory.Create();
        return (new ChatService(db, NullLogger<ChatService>.Instance), db);
    }

    private static AppUser MakeUser(string displayName = "TestUser") => new()
    {
        Id = Guid.NewGuid(),
        Email = $"{Guid.NewGuid()}@test.com",
        DisplayName = displayName,
        Gender = Gender.Man,
        Birthdate = new DateOnly(1995, 1, 1),
        PasswordHash = "hash",
        CreatedAt = DateTime.UtcNow
    };

    /// <summary>Seeds two users, a match between them, and the associated chat.</summary>
    private static async Task<(Match match, Chat chat, AppUser userA, AppUser userB)> SeedMatchWithChatAsync(AppDbContext db)
    {
        var userA = MakeUser("Alice");
        var userB = MakeUser("Bob");
        db.Users.AddRange(userA, userB);

        var match = new Match
        {
            Id = Guid.NewGuid(),
            UserAId = userA.Id,
            UserBId = userB.Id,
            CreatedAt = DateTime.UtcNow
        };
        var chat = new Chat { Id = Guid.NewGuid(), MatchId = match.Id };

        db.Matches.Add(match);
        db.Chats.Add(chat);
        await db.SaveChangesAsync();

        return (match, chat, userA, userB);
    }

    // ── GetUserChatsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserChatsAsync_NoMatches_ReturnsEmpty()
    {
        var (svc, _) = CreateService();

        var result = await svc.GetUserChatsAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserChatsAsync_WithMatch_ReturnsChatSummaryForUserA()
    {
        var (svc, db) = CreateService();
        var (match, chat, userA, userB) = await SeedMatchWithChatAsync(db);

        var result = await svc.GetUserChatsAsync(userA.Id);

        result.Should().HaveCount(1);
        result[0].ChatId.Should().Be(chat.Id);
        result[0].MatchId.Should().Be(match.Id);
        result[0].OtherUserId.Should().Be(userB.Id);
        result[0].OtherUserDisplayName.Should().Be("Bob");
    }

    [Fact]
    public async Task GetUserChatsAsync_WithMatch_ReturnsChatSummaryForUserB()
    {
        var (svc, db) = CreateService();
        var (_, chat, userA, userB) = await SeedMatchWithChatAsync(db);

        var result = await svc.GetUserChatsAsync(userB.Id);

        result.Should().HaveCount(1);
        result[0].OtherUserId.Should().Be(userA.Id);
        result[0].OtherUserDisplayName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetUserChatsAsync_ShortMessage_ShowsFullSnippet()
    {
        var (svc, db) = CreateService();
        var (_, chat, userA, _) = await SeedMatchWithChatAsync(db);

        db.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chat.Id,
            SenderUserId = userA.Id,
            Content = "Hello!",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await svc.GetUserChatsAsync(userA.Id);

        result[0].LastMessageSnippet.Should().Be("Hello!");
    }

    [Fact]
    public async Task GetUserChatsAsync_LongMessage_TruncatesSnippetAt50Chars()
    {
        var (svc, db) = CreateService();
        var (_, chat, userA, _) = await SeedMatchWithChatAsync(db);
        var longContent = new string('x', 60);

        db.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chat.Id,
            SenderUserId = userA.Id,
            Content = longContent,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await svc.GetUserChatsAsync(userA.Id);

        result[0].LastMessageSnippet.Should().EndWith("...");
        result[0].LastMessageSnippet!.Length.Should().Be(53); // 50 chars + "..."
    }

    [Fact]
    public async Task GetUserChatsAsync_NoMessages_SnippetIsNull()
    {
        var (svc, db) = CreateService();
        var (_, _, userA, _) = await SeedMatchWithChatAsync(db);

        var result = await svc.GetUserChatsAsync(userA.Id);

        result[0].LastMessageSnippet.Should().BeNull();
        result[0].LastMessageAt.Should().BeNull();
    }

    // ── GetMessagesAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetMessagesAsync_EmptyChat_ReturnsEmpty()
    {
        var (svc, db) = CreateService();
        var (_, chat, userA, _) = await SeedMatchWithChatAsync(db);

        var result = await svc.GetMessagesAsync(chat.Id, userA.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessagesAsync_WithMessages_ReturnsChronologicalOrder()
    {
        var (svc, db) = CreateService();
        var (_, chat, userA, userB) = await SeedMatchWithChatAsync(db);
        var t = DateTime.UtcNow;

        db.Messages.AddRange(
            new Message { Id = Guid.NewGuid(), ChatId = chat.Id, SenderUserId = userB.Id, Content = "first",  CreatedAt = t },
            new Message { Id = Guid.NewGuid(), ChatId = chat.Id, SenderUserId = userA.Id, Content = "second", CreatedAt = t.AddMinutes(1) }
        );
        await db.SaveChangesAsync();

        var result = await svc.GetMessagesAsync(chat.Id, userA.Id);

        result.Should().HaveCount(2);
        result[0].Content.Should().Be("first");
        result[1].Content.Should().Be("second");
    }

    [Fact]
    public async Task GetMessagesAsync_NonParticipant_ThrowsUnauthorizedAccessException()
    {
        var (svc, db) = CreateService();
        var (_, chat, _, _) = await SeedMatchWithChatAsync(db);

        var act = () => svc.GetMessagesAsync(chat.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*not a participant*");
    }

    [Fact]
    public async Task GetMessagesAsync_ChatNotFound_ThrowsKeyNotFoundException()
    {
        var (svc, _) = CreateService();

        var act = () => svc.GetMessagesAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Chat not found*");
    }

    // ── SendMessageAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessageAsync_ValidMessage_PersistsAndReturnsDto()
    {
        var (svc, db) = CreateService();
        var (_, chat, userA, _) = await SeedMatchWithChatAsync(db);

        var result = await svc.SendMessageAsync(chat.Id, userA.Id, new SendMessageRequest("Hello!"));

        result.Content.Should().Be("Hello!");
        result.SenderUserId.Should().Be(userA.Id);
        result.ChatId.Should().Be(chat.Id);
        db.Messages.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendMessageAsync_TrimsWhitespace()
    {
        var (svc, db) = CreateService();
        var (_, chat, userA, _) = await SeedMatchWithChatAsync(db);

        var result = await svc.SendMessageAsync(chat.Id, userA.Id, new SendMessageRequest("  Hello  "));

        result.Content.Should().Be("Hello");
    }

    [Fact]
    public async Task SendMessageAsync_WhitespaceOnly_ThrowsArgumentException()
    {
        var (svc, db) = CreateService();
        var (_, chat, userA, _) = await SeedMatchWithChatAsync(db);

        var act = () => svc.SendMessageAsync(chat.Id, userA.Id, new SendMessageRequest("   "));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public async Task SendMessageAsync_NonParticipant_ThrowsUnauthorizedAccessException()
    {
        var (svc, db) = CreateService();
        var (_, chat, _, _) = await SeedMatchWithChatAsync(db);

        var act = () => svc.SendMessageAsync(chat.Id, Guid.NewGuid(), new SendMessageRequest("Hi"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── EnsureAccessAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureAccessAsync_ChatNotFound_ThrowsKeyNotFoundException()
    {
        var (svc, _) = CreateService();

        var act = () => svc.EnsureAccessAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Chat not found*");
    }

    [Fact]
    public async Task EnsureAccessAsync_NonParticipant_ThrowsUnauthorizedAccessException()
    {
        var (svc, db) = CreateService();
        var (_, chat, _, _) = await SeedMatchWithChatAsync(db);

        var act = () => svc.EnsureAccessAsync(chat.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*not a participant*");
    }

    [Fact]
    public async Task EnsureAccessAsync_ValidParticipant_DoesNotThrow()
    {
        var (svc, db) = CreateService();
        var (_, chat, userA, _) = await SeedMatchWithChatAsync(db);

        var act = () => svc.EnsureAccessAsync(chat.Id, userA.Id);

        await act.Should().NotThrowAsync();
    }
}
