using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RatingApp.Api.IntegrationTests.Helpers;

namespace RatingApp.Api.IntegrationTests;

public class ProfileIntegrationTests : ApiTestBase
{
    [Fact]
    public async Task GetProfile_Authenticated_Returns200()
    {
        var (token, _) = await RegisterAndGetTokenAsync("profile@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("displayName");
    }

    [Fact]
    public async Task GetProfile_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_ValidRequest_UpdatesDisplayName()
    {
        var (token, _) = await RegisterAndGetTokenAsync("update@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.PutAsJsonAsync("/api/me", new
        {
            displayName = "Updated Name",
            latitude = 51.5,
            longitude = -0.12
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Updated Name");
    }

    [Fact]
    public async Task GetPreferences_Authenticated_Returns200()
    {
        var (token, _) = await RegisterAndGetTokenAsync("prefs@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.GetAsync("/api/me/preferences");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePreferences_ValidRequest_Returns200()
    {
        var (token, _) = await RegisterAndGetTokenAsync("saveprefs@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.PutAsJsonAsync("/api/me/preferences", new
        {
            preferredGender = 2,
            minAge = 21,
            maxAge = 40,
            maxDistanceMiles = 75
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("75");
    }

    [Fact]
    public async Task GetRatingSummary_NewUser_ReturnsZero()
    {
        var (token, _) = await RegisterAndGetTokenAsync("summary@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.GetAsync("/api/me/rating-summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ratingCount\":0");
    }
}
