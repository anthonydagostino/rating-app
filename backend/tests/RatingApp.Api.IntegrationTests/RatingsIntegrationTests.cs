using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RatingApp.Api.IntegrationTests.Helpers;

namespace RatingApp.Api.IntegrationTests;

public class RatingsIntegrationTests : ApiTestBase
{
    // ── POST /api/ratings ────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitRating_ValidScore_Returns200()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("valid@test.com", gender: 1);
        var (_, userBId) = await RegisterAndGetTokenAsync("validB@test.com", gender: 2);
        var authed = CreateAuthenticatedClient(tokenA);

        var response = await authed.PostAsJsonAsync("/api/ratings", RatingPayload(userBId, 7));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitRating_BothAboveThreshold_MatchCreatedIsTrue()
    {
        var (tokenA, userAId) = await RegisterAndGetTokenAsync("matchA@test.com", gender: 1);
        var (tokenB, userBId) = await RegisterAndGetTokenAsync("matchB@test.com", gender: 2);

        var authedA = CreateAuthenticatedClient(tokenA);
        await authedA.PostAsJsonAsync("/api/ratings", RatingPayload(userBId, 8));

        var authedB = CreateAuthenticatedClient(tokenB);
        var response = await authedB.PostAsJsonAsync("/api/ratings", RatingPayload(userAId, 9));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("true"); // matchCreated: true
    }

    [Fact]
    public async Task SubmitRating_BelowThreshold_MatchCreatedIsFalse()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("raterA@test.com", gender: 1);
        var (_, userBId) = await RegisterAndGetTokenAsync("raterB@test.com", gender: 2);

        var authedA = CreateAuthenticatedClient(tokenA);
        var response = await authedA.PostAsJsonAsync("/api/ratings", RatingPayload(userBId, 5));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("false"); // matchCreated: false
    }

    [Fact]
    public async Task SubmitRating_ScoreOutOfRange_Returns400()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("rangeA@test.com", gender: 1);
        var (_, userBId) = await RegisterAndGetTokenAsync("rangeB@test.com", gender: 2);
        var authed = CreateAuthenticatedClient(tokenA);

        var tooHigh = await authed.PostAsJsonAsync("/api/ratings", RatingPayload(userBId, 11));
        tooHigh.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var tooLow = await authed.PostAsJsonAsync("/api/ratings", RatingPayload(userBId, 0));
        tooLow.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitRating_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/ratings", RatingPayload(Guid.NewGuid(), 8));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SubmitRating_Self_Returns400()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("selfRate@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.PostAsJsonAsync("/api/ratings", RatingPayload(userId, 8));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/ratings/summary/{userId} ────────────────────────────────────

    [Fact]
    public async Task GetSummary_UserWithRatings_ReturnsAverageAndPercentile()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("summaryA@test.com", gender: 1);
        var (_, userBId) = await RegisterAndGetTokenAsync("summaryB@test.com", gender: 2);
        var authedA = CreateAuthenticatedClient(tokenA);

        await authedA.PostAsJsonAsync("/api/ratings", RatingPayload(userBId, 8));

        var response = await authedA.GetAsync($"/api/ratings/summary/{userBId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("averageScore");
        content.Should().Contain("ratingCount");
        content.Should().Contain("percentile");
    }

    [Fact]
    public async Task GetSummary_UserWithNoRatings_ReturnsZeros()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("noRatingsUser@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.GetAsync($"/api/ratings/summary/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"ratingCount\":0");
        content.Should().Contain("\"averageScore\":0");
    }
}
