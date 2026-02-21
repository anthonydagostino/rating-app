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
        var response = await authedA.PostAsJsonAsync("/api/ratings", RatingPayload(userBId, 5));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("false"); // matchCreated: false
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
    public async Task SubmitRating_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/ratings", RatingPayload(Guid.NewGuid(), 8));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCriteria_ReturnsActiveList()
    {
        var (token, _) = await RegisterAndGetTokenAsync("criteriaUser@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.GetAsync("/api/ratings/criteria");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Skill");
    }

    [Fact]
    public async Task GetAggregate_ReturnsAggregateForCandidate()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("aggA@test.com", gender: 1);
        var (tokenB, userBId) = await RegisterAndGetTokenAsync("aggB@test.com", gender: 2);

        var authedA = CreateAuthenticatedClient(tokenA);
        await authedA.PostAsJsonAsync("/api/ratings", RatingPayload(userBId, 8));

        var response = await authedA.GetAsync($"/api/ratings/candidates/{userBId}/aggregate");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("weightedAggregate");
    }

    // ── Validation → 400 ────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitRating_EmptyCriteriaList_Returns400()
    {
        var (token, _) = await RegisterAndGetTokenAsync("emptyC@test.com");
        var authed = CreateAuthenticatedClient(token);

        var payload = new { ratedUserId = Guid.NewGuid(), criteria = Array.Empty<object>(), comment = (string?)null };
        var response = await authed.PostAsJsonAsync("/api/ratings", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitRating_ScoreOutOfRange_Returns400()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("rangeA@test.com", gender: 1);
        var (_, userBId) = await RegisterAndGetTokenAsync("rangeB@test.com", gender: 2);
        var authed = CreateAuthenticatedClient(tokenA);

        var payload = new
        {
            ratedUserId = userBId,
            criteria = new[] { new { criterionId = SkillCriterionId, score = 11 } },
            comment = (string?)null
        };
        var response = await authed.PostAsJsonAsync("/api/ratings", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitRating_DuplicateCriterionIds_Returns400()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("dupA@test.com", gender: 1);
        var (_, userBId) = await RegisterAndGetTokenAsync("dupB@test.com", gender: 2);
        var authed = CreateAuthenticatedClient(tokenA);

        var payload = new
        {
            ratedUserId = userBId,
            criteria = new[]
            {
                new { criterionId = SkillCriterionId, score = 8 },
                new { criterionId = SkillCriterionId, score = 7 }
            },
            comment = (string?)null
        };
        var response = await authed.PostAsJsonAsync("/api/ratings", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Unauthenticated → 401 ────────────────────────────────────────────────

    [Fact]
    public async Task GetCriteria_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/ratings/criteria");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAggregate_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync($"/api/ratings/candidates/{Guid.NewGuid()}/aggregate");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Edge cases ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAggregate_NoRatings_ReturnsZeroAggregateAndTotalRatings()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("noRatingsUser@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.GetAsync($"/api/ratings/candidates/{userId}/aggregate");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"totalRatings\":0");
        content.Should().Contain("\"weightedAggregate\":0");
    }

    [Fact]
    public async Task SubmitRating_WithComment_Returns200()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("cmtA@test.com", gender: 1);
        var (_, userBId) = await RegisterAndGetTokenAsync("cmtB@test.com", gender: 2);
        var authed = CreateAuthenticatedClient(tokenA);

        var payload = new
        {
            ratedUserId = userBId,
            criteria = new[]
            {
                new { criterionId = SkillCriterionId,   score = 8 },
                new { criterionId = CommCriterionId,    score = 7 },
                new { criterionId = CultureCriterionId, score = 9 }
            },
            comment = "Great person to work with!"
        };
        var response = await authed.PostAsJsonAsync("/api/ratings", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
