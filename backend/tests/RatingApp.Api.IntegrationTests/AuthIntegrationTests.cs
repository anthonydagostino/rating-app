using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RatingApp.Api.IntegrationTests.Helpers;

namespace RatingApp.Api.IntegrationTests;

public class AuthIntegrationTests : ApiTestBase
{
    private object ValidRegisterBody(string email = "new@test.com") => new
    {
        email,
        password = "Password123!",
        displayName = "TestUser",
        gender = 1,
        birthdate = "1995-06-15",
        latitude = 40.7128,
        longitude = -74.0060
    };

    [Fact]
    public async Task Register_ValidRequest_Returns200WithToken()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", ValidRegisterBody());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("token");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        await Client.PostAsJsonAsync("/api/auth/register", ValidRegisterBody("dupe@test.com"));
        var response = await Client.PostAsJsonAsync("/api/auth/register", ValidRegisterBody("dupe@test.com"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        await Client.PostAsJsonAsync("/api/auth/register", ValidRegisterBody("login@test.com"));

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "login@test.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await Client.PostAsJsonAsync("/api/auth/register", ValidRegisterBody("auth@test.com"));

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "auth@test.com",
            password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@test.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
