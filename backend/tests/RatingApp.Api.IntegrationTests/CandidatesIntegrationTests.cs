using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RatingApp.Api.IntegrationTests.Helpers;

namespace RatingApp.Api.IntegrationTests;

public class CandidatesIntegrationTests : ApiTestBase
{
    [Fact]
    public async Task GetCandidates_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/candidates");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCandidates_NoOtherUsers_ReturnsEmptyArray()
    {
        // Male user, looking for females. No females in DB.
        var (token, _) = await RegisterAndGetTokenAsync("alone@test.com", gender: 1);
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.GetAsync("/api/candidates?pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var candidates = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCandidates_MatchingCandidateWithPhoto_ReturnsThem()
    {
        // Male user registered at same lat/lon
        var (token, _) = await RegisterAndGetTokenAsync("male@test.com", gender: 1,
            lat: 40.7128, lon: -74.0060);

        // Seed a Female user with a photo at same location
        await SeedUserWithPhotoAsync(gender: 2, lat: 40.7128, lon: -74.0060);

        var authed = CreateAuthenticatedClient(token);
        var response = await authed.GetAsync("/api/candidates?pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var candidates = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        candidates.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCandidates_CandidateWithoutPhoto_NotReturned()
    {
        var (token, _) = await RegisterAndGetTokenAsync("nophoto@test.com", gender: 1,
            lat: 40.7128, lon: -74.0060);

        // Seed female user with NO photo (SeedUserWithPhotoAsync always adds a photo,
        // so we use RegisterAndGetTokenAsync for a user without photo)
        await RegisterAndGetTokenAsync("femalenoPhoto@test.com", gender: 2,
            lat: 40.7128, lon: -74.0060);

        var authed = CreateAuthenticatedClient(token);
        var response = await authed.GetAsync("/api/candidates?pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var candidates = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        candidates.Should().BeEmpty();
    }
}
