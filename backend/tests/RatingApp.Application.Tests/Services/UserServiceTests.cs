using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RatingApp.Application.DTOs.Users;
using RatingApp.Application.Services;
using RatingApp.Application.Tests.Helpers;
using RatingApp.Domain.Entities;
using RatingApp.Domain.Enums;

namespace RatingApp.Application.Tests.Services;

public class UserServiceTests
{
    private static (UserService svc, RatingApp.Infrastructure.Persistence.AppDbContext db) CreateService()
    {
        var db = InMemoryDbFactory.Create();
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApiBaseUrl"] = "http://localhost:5212" })
            .Build();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var photoSvc = new PhotoService(db, envMock.Object, config, httpContextAccessor.Object);
        return (new UserService(db, photoSvc), db);
    }

    private static AppUser MakeUser(Guid? id = null)
    {
        var userId = id ?? Guid.NewGuid();
        return new AppUser
        {
            Id = userId,
            Email = $"{userId}@test.com",
            DisplayName = "Original Name",
            Gender = Gender.Man,
            Birthdate = new DateOnly(1995, 6, 15),
            Latitude = 40.7,
            Longitude = -74.0,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            Preference = new UserPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PreferredGender = Gender.Woman,
                MinAge = 18,
                MaxAge = 35,
                MaxDistanceMiles = 50
            }
        };
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsCorrectProfile()
    {
        var (svc, db) = CreateService();
        var user = MakeUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var profile = await svc.GetProfileAsync(user.Id);

        profile.Id.Should().Be(user.Id);
        profile.Email.Should().Be(user.Email);
        profile.DisplayName.Should().Be("Original Name");
    }

    [Fact]
    public async Task GetProfileAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        var (svc, _) = CreateService();

        var act = () => svc.GetProfileAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesDisplayNameAndCoordinates()
    {
        var (svc, db) = CreateService();
        var user = MakeUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var updated = await svc.UpdateProfileAsync(user.Id,
            new UpdateProfileRequest("New Name", 51.5, -0.12));

        updated.DisplayName.Should().Be("New Name");
        updated.Latitude.Should().Be(51.5);
        updated.Longitude.Should().Be(-0.12);
    }

    [Fact]
    public async Task GetPreferencesAsync_ReturnsExistingPreferences()
    {
        var (svc, db) = CreateService();
        var user = MakeUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var prefs = await svc.GetPreferencesAsync(user.Id);

        prefs.PreferredGender.Should().Be((int)Gender.Woman);
        prefs.MinAge.Should().Be(18);
        prefs.MaxAge.Should().Be(35);
    }

    [Fact]
    public async Task GetPreferencesAsync_NoPrefs_ThrowsKeyNotFoundException()
    {
        var (svc, db) = CreateService();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "nopref@test.com",
            DisplayName = "No Pref",
            Gender = Gender.Man,
            Birthdate = new DateOnly(1995, 1, 1),
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var act = () => svc.GetPreferencesAsync(user.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpsertPreferencesAsync_UpdatesExistingPreferences()
    {
        var (svc, db) = CreateService();
        var user = MakeUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await svc.UpsertPreferencesAsync(user.Id,
            new PreferenceDto(1, 21, 40, 100));

        result.MinAge.Should().Be(21);
        result.MaxAge.Should().Be(40);
        result.MaxDistanceMiles.Should().Be(100);
    }

    [Fact]
    public async Task UpsertPreferencesAsync_CreatesPrefsWhenMissing()
    {
        var (svc, db) = CreateService();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "newpref@test.com",
            DisplayName = "New",
            Gender = Gender.Man,
            Birthdate = new DateOnly(1995, 1, 1),
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await svc.UpsertPreferencesAsync(user.Id,
            new PreferenceDto(2, 20, 30, 25));

        result.PreferredGender.Should().Be(2);
        db.UserPreferences.Should().ContainSingle(p => p.UserId == user.Id);
    }
}
