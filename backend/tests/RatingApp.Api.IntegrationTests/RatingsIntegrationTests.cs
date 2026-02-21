using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RatingApp.Api.IntegrationTests.Helpers;

namespace RatingApp.Api.IntegrationTests;

public class RatingsIntegrationTests : ApiTestBase
{
    private object RatingPayload(Guid ratedUserId, int score) => new
    {
        ratedUserId,
        criteria = new[]
        {
            new { criterionId = SkillCriterionId,   score },
            new { criterionId = CommCriterionId,    score },
            new { criterionId = CultureCriterionId, score }
        },
        comment = (string?)null
    };

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
}
