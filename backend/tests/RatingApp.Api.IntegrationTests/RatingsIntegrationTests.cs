using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RatingApp.Api.IntegrationTests.Helpers;

namespace RatingApp.Api.IntegrationTests;

public class RatingsIntegrationTests : ApiTestBase
{
    [Fact]
    public async Task SubmitRating_BelowThreshold_MatchCreatedIsFalse()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("raterA@test.com", gender: 1);
        var (_, userBId) = await RegisterAndGetTokenAsync("raterB@test.com", gender: 2);

        var authedA = CreateAuthenticatedClient(tokenA);
        var response = await authedA.PostAsJsonAsync("/api/ratings", new
        {
            ratedUserId = userBId,
            score = 5
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("false"); // matchCreated: false
    }

    [Fact]
    public async Task SubmitRating_BothAboveThreshold_MatchCreatedIsTrue()
    {
        var (tokenA, userAId) = await RegisterAndGetTokenAsync("matchA@test.com", gender: 1);
        var (tokenB, userBId) = await RegisterAndGetTokenAsync("matchB@test.com", gender: 2);

        // A rates B high
        var authedA = CreateAuthenticatedClient(tokenA);
        await authedA.PostAsJsonAsync("/api/ratings", new { ratedUserId = userBId, score = 8 });

        // B rates A high — should create match
        var authedB = CreateAuthenticatedClient(tokenB);
        var response = await authedB.PostAsJsonAsync("/api/ratings", new { ratedUserId = userAId, score = 9 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("true"); // matchCreated: true
    }

    [Fact]
    public async Task SubmitRating_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/ratings", new
        {
            ratedUserId = Guid.NewGuid(),
            score = 8
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
