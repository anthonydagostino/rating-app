using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RatingApp.Application.DTOs.Auth;
using RatingApp.Application.Services;
using RatingApp.Application.Tests.Helpers;
using RatingApp.Domain.Interfaces;
using RatingApp.Infrastructure.Security;

namespace RatingApp.Application.Tests.Services;

public class AuthServiceTests
{
    private static AuthService CreateService(out Mock<IJwtTokenGenerator> jwtMock)
    {
        var db = InMemoryDbFactory.Create();
        var hasher = new PasswordHasher();
        jwtMock = new Mock<IJwtTokenGenerator>();
        jwtMock.Setup(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
               .Returns("fake-jwt-token");
        return new AuthService(db, hasher, jwtMock.Object, NullLogger<AuthService>.Instance);
    }

    private static RegisterRequest ValidRegisterRequest(string email = "user@test.com") =>
        new(email, "Password123!", "TestUser", 1, new DateOnly(1995, 6, 15), 40.7, -74.0);

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsAuthResponse()
    {
        var svc = CreateService(out _);

        var result = await svc.RegisterAsync(ValidRegisterRequest());

        result.Token.Should().Be("fake-jwt-token");
        result.Email.Should().Be("user@test.com");
        result.DisplayName.Should().Be("TestUser");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var svc = CreateService(out _);
        await svc.RegisterAsync(ValidRegisterRequest("dupe@test.com"));

        var act = () => svc.RegisterAsync(ValidRegisterRequest("dupe@test.com"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task RegisterAsync_EmailStoredLowercase()
    {
        var svc = CreateService(out _);

        var result = await svc.RegisterAsync(ValidRegisterRequest("UPPER@TEST.COM"));

        result.Email.Should().Be("upper@test.com");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var svc = CreateService(out _);
        await svc.RegisterAsync(ValidRegisterRequest("login@test.com"));

        var result = await svc.LoginAsync(new LoginRequest("login@test.com", "Password123!"));

        result.Token.Should().Be("fake-jwt-token");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var svc = CreateService(out _);
        await svc.RegisterAsync(ValidRegisterRequest("auth@test.com"));

        var act = () => svc.LoginAsync(new LoginRequest("auth@test.com", "WrongPassword!"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        var svc = CreateService(out _);

        var act = () => svc.LoginAsync(new LoginRequest("nobody@test.com", "Password123!"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
