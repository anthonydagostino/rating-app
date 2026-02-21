using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RatingApp.Api.IntegrationTests.Helpers;

namespace RatingApp.Api.IntegrationTests;

public class ChatsIntegrationTests : ApiTestBase
{
    // ── GET /api/chats ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetChats_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/chats");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetChats_NoMatches_ReturnsEmptyArray()
    {
        var (token, _) = await RegisterAndGetTokenAsync("chats-none@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.GetAsync("/api/chats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("[]");
    }

    [Fact]
    public async Task GetChats_AfterMutualHighRating_ReturnsChatSummary()
    {
        var (tokenA, userAId) = await RegisterAndGetTokenAsync("chats-a@test.com", gender: 1);
        var (tokenB, userBId) = await RegisterAndGetTokenAsync("chats-b@test.com", gender: 2);

        // Mutual high ratings → creates match + chat
        var authedA = CreateAuthenticatedClient(tokenA);
        var authedB = CreateAuthenticatedClient(tokenB);
        await authedA.PostAsJsonAsync("/api/ratings", new { ratedUserId = userBId, score = 8 });
        await authedB.PostAsJsonAsync("/api/ratings", new { ratedUserId = userAId, score = 9 });

        var response = await authedA.GetAsync("/api/chats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("chatId");
        body.Should().Contain("otherUserDisplayName");
    }

    // ── GET /api/chats/{chatId}/messages ──────────────────────────────────────

    [Fact]
    public async Task GetMessages_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync($"/api/chats/{Guid.NewGuid()}/messages");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMessages_NonExistentChat_Returns404()
    {
        var (token, _) = await RegisterAndGetTokenAsync("msgs-notfound@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.GetAsync($"/api/chats/{Guid.NewGuid()}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMessages_NotAParticipant_Returns403()
    {
        // Create a match between A and B
        var (tokenA, userAId) = await RegisterAndGetTokenAsync("msgs-403a@test.com", gender: 1);
        var (tokenB, userBId) = await RegisterAndGetTokenAsync("msgs-403b@test.com", gender: 2);
        var (tokenC, _) = await RegisterAndGetTokenAsync("msgs-403c@test.com", gender: 1);

        var authedA = CreateAuthenticatedClient(tokenA);
        var authedB = CreateAuthenticatedClient(tokenB);
        await authedA.PostAsJsonAsync("/api/ratings", new { ratedUserId = userBId, score = 8 });
        await authedB.PostAsJsonAsync("/api/ratings", new { ratedUserId = userAId, score = 8 });

        // Get chat ID
        var chatsResponse = await authedA.GetAsync("/api/chats");
        var chats = await chatsResponse.Content.ReadFromJsonAsync<List<ChatSummaryResponse>>()
            ?? throw new InvalidOperationException("No chats returned");
        var chatId = chats[0].ChatId;

        // User C tries to read the chat between A and B
        var authedC = CreateAuthenticatedClient(tokenC);
        var response = await authedC.GetAsync($"/api/chats/{chatId}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMessages_EmptyChat_ReturnsEmptyArray()
    {
        var (tokenA, userAId) = await RegisterAndGetTokenAsync("msgs-empty-a@test.com", gender: 1);
        var (tokenB, userBId) = await RegisterAndGetTokenAsync("msgs-empty-b@test.com", gender: 2);

        var authedA = CreateAuthenticatedClient(tokenA);
        var authedB = CreateAuthenticatedClient(tokenB);
        await authedA.PostAsJsonAsync("/api/ratings", new { ratedUserId = userBId, score = 8 });
        await authedB.PostAsJsonAsync("/api/ratings", new { ratedUserId = userAId, score = 8 });

        var chats = await (await authedA.GetAsync("/api/chats"))
            .Content.ReadFromJsonAsync<List<ChatSummaryResponse>>()
            ?? throw new InvalidOperationException();
        var chatId = chats[0].ChatId;

        var response = await authedA.GetAsync($"/api/chats/{chatId}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("[]");
    }

    // ── POST /api/chats/{chatId}/messages ─────────────────────────────────────

    [Fact]
    public async Task SendMessage_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/chats/{Guid.NewGuid()}/messages",
            new { content = "Hello" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendMessage_ValidMessage_Returns200WithMessageDto()
    {
        var (tokenA, userAId) = await RegisterAndGetTokenAsync("send-msg-a@test.com", gender: 1);
        var (tokenB, userBId) = await RegisterAndGetTokenAsync("send-msg-b@test.com", gender: 2);

        var authedA = CreateAuthenticatedClient(tokenA);
        var authedB = CreateAuthenticatedClient(tokenB);
        await authedA.PostAsJsonAsync("/api/ratings", new { ratedUserId = userBId, score = 9 });
        await authedB.PostAsJsonAsync("/api/ratings", new { ratedUserId = userAId, score = 9 });

        var chats = await (await authedA.GetAsync("/api/chats"))
            .Content.ReadFromJsonAsync<List<ChatSummaryResponse>>()
            ?? throw new InvalidOperationException();
        var chatId = chats[0].ChatId;

        var response = await authedA.PostAsJsonAsync(
            $"/api/chats/{chatId}/messages",
            new { content = "Hello!" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Hello!");
        body.Should().Contain("content");
    }

    [Fact]
    public async Task SendMessage_ThenGetMessages_MessageAppears()
    {
        var (tokenA, userAId) = await RegisterAndGetTokenAsync("msg-flow-a@test.com", gender: 1);
        var (tokenB, userBId) = await RegisterAndGetTokenAsync("msg-flow-b@test.com", gender: 2);

        var authedA = CreateAuthenticatedClient(tokenA);
        var authedB = CreateAuthenticatedClient(tokenB);
        await authedA.PostAsJsonAsync("/api/ratings", new { ratedUserId = userBId, score = 9 });
        await authedB.PostAsJsonAsync("/api/ratings", new { ratedUserId = userAId, score = 9 });

        var chats = await (await authedA.GetAsync("/api/chats"))
            .Content.ReadFromJsonAsync<List<ChatSummaryResponse>>()
            ?? throw new InvalidOperationException();
        var chatId = chats[0].ChatId;

        await authedA.PostAsJsonAsync($"/api/chats/{chatId}/messages", new { content = "Hey there!" });

        var messagesResponse = await authedA.GetAsync($"/api/chats/{chatId}/messages");
        var body = await messagesResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Hey there!");
    }

    [Fact]
    public async Task SendMessage_EmptyContent_Returns400()
    {
        var (tokenA, userAId) = await RegisterAndGetTokenAsync("send-empty-a@test.com", gender: 1);
        var (tokenB, userBId) = await RegisterAndGetTokenAsync("send-empty-b@test.com", gender: 2);

        var authedA = CreateAuthenticatedClient(tokenA);
        var authedB = CreateAuthenticatedClient(tokenB);
        await authedA.PostAsJsonAsync("/api/ratings", new { ratedUserId = userBId, score = 9 });
        await authedB.PostAsJsonAsync("/api/ratings", new { ratedUserId = userAId, score = 9 });

        var chats = await (await authedA.GetAsync("/api/chats"))
            .Content.ReadFromJsonAsync<List<ChatSummaryResponse>>()
            ?? throw new InvalidOperationException();
        var chatId = chats[0].ChatId;

        var response = await authedA.PostAsJsonAsync(
            $"/api/chats/{chatId}/messages",
            new { content = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private record ChatSummaryResponse(Guid ChatId, Guid MatchId, Guid OtherUserId, string OtherUserDisplayName);
}
