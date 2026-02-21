using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RatingApp.Api.IntegrationTests.Helpers;

namespace RatingApp.Api.IntegrationTests;

public class SessionsIntegrationTests : ApiTestBase
{
    // ── CREATE ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSession_ValidRequest_Returns201WithSessionDto()
    {
        var (token, _) = await RegisterAndGetTokenAsync("creator@test.com", gender: 1);
        var (_, candidateId) = await RegisterAndGetTokenAsync("candidate@test.com", gender: 2);

        var client = CreateAuthenticatedClient(token);
        var response = await client.PostAsJsonAsync("/api/sessions", new
        {
            candidateId,
            title = "Panel review"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Panel review");
        body.Should().Contain("Active");
    }

    [Fact]
    public async Task CreateSession_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/sessions", new
        {
            candidateId = Guid.NewGuid(),
            title = "Anon session"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateSession_UnknownCandidate_Returns404()
    {
        var (token, _) = await RegisterAndGetTokenAsync("creator2@test.com", gender: 1);
        var client = CreateAuthenticatedClient(token);

        var response = await client.PostAsJsonAsync("/api/sessions", new
        {
            candidateId = Guid.NewGuid(), // does not exist
            title = "Ghost candidate"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSession_AsCreator_ReturnsSessionDto()
    {
        var (token, _) = await RegisterAndGetTokenAsync("getCreator@test.com", gender: 1);
        var (_, candidateId) = await RegisterAndGetTokenAsync("getCandidate@test.com", gender: 2);
        var client = CreateAuthenticatedClient(token);

        var created = await (await client.PostAsJsonAsync("/api/sessions", new { candidateId }))
            .Content.ReadFromJsonAsync<SessionResponse>();

        var getResponse = await client.GetAsync($"/api/sessions/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadAsStringAsync();
        body.Should().Contain(created.Id.ToString());
    }

    [Fact]
    public async Task GetSession_AsNonParticipant_Returns403()
    {
        var (creatorToken, _) = await RegisterAndGetTokenAsync("owner@test.com", gender: 1);
        var (_, candidateId) = await RegisterAndGetTokenAsync("cand2@test.com", gender: 2);
        var (strangerToken, _) = await RegisterAndGetTokenAsync("stranger@test.com", gender: 1);

        var creator = CreateAuthenticatedClient(creatorToken);
        var created = await (await creator.PostAsJsonAsync("/api/sessions", new { candidateId }))
            .Content.ReadFromJsonAsync<SessionResponse>();

        var stranger = CreateAuthenticatedClient(strangerToken);
        var response = await stranger.GetAsync($"/api/sessions/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── LOCK / FINALIZE ──────────────────────────────────────────────────────

    [Fact]
    public async Task LockSession_AsCreator_Returns204()
    {
        var (token, _) = await RegisterAndGetTokenAsync("lockCreator@test.com", gender: 1);
        var (_, candidateId) = await RegisterAndGetTokenAsync("lockCand@test.com", gender: 2);
        var client = CreateAuthenticatedClient(token);

        var created = await (await client.PostAsJsonAsync("/api/sessions", new { candidateId }))
            .Content.ReadFromJsonAsync<SessionResponse>();

        var lockResponse = await client.PostAsync($"/api/sessions/{created!.Id}/lock", null);
        lockResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LockSession_AsNonCreator_Returns403()
    {
        var (creatorToken, _) = await RegisterAndGetTokenAsync("lockOwner@test.com", gender: 1);
        var (_, candidateId) = await RegisterAndGetTokenAsync("lockCand2@test.com", gender: 2);
        var (otherToken, otherId) = await RegisterAndGetTokenAsync("lockOther@test.com", gender: 1);

        var creator = CreateAuthenticatedClient(creatorToken);
        var created = await (await creator.PostAsJsonAsync("/api/sessions", new { candidateId }))
            .Content.ReadFromJsonAsync<SessionResponse>();

        // Seed other user as participant directly via DB to bypass hub
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RatingApp.Infrastructure.Persistence.AppDbContext>();
        db.SessionParticipantRatings.Add(new RatingApp.Domain.Entities.SessionParticipantRating
        {
            Id = Guid.NewGuid(),
            SessionId = created!.Id,
            RaterUserId = otherId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var other = CreateAuthenticatedClient(otherToken);
        var response = await other.PostAsync($"/api/sessions/{created.Id}/lock", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FinalizeSession_PersistsAggregateRating()
    {
        var (creatorToken, creatorId) = await RegisterAndGetTokenAsync("finCreator@test.com", gender: 1);
        var (_, candidateId) = await RegisterAndGetTokenAsync("finCand@test.com", gender: 2);
        var client = CreateAuthenticatedClient(creatorToken);

        // Create session
        var created = await (await client.PostAsJsonAsync("/api/sessions", new { candidateId }))
            .Content.ReadFromJsonAsync<SessionResponse>();

        // Submit a rating directly via DB (avoids hub connection in integration test)
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RatingApp.Infrastructure.Persistence.AppDbContext>();
        var participation = await db.SessionParticipantRatings
            .FirstAsync(r => r.SessionId == created!.Id && r.RaterUserId == creatorId);
        participation.Score = 8;
        participation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Finalize
        var finalizeResponse = await client.PostAsync($"/api/sessions/{created!.Id}/finalize", null);
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify aggregate rating was persisted
        var rating = db.Ratings.FirstOrDefault(r =>
            r.RaterUserId == creatorId && r.RatedUserId == candidateId);
        rating.Should().NotBeNull();
        rating!.Score.Should().Be(8);
    }

    // Helper record for deserializing created session
    private record SessionResponse(Guid Id, string Status, string? Title);
}