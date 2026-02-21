using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using RatingApp.Application.Services;
using RatingApp.Application.Tests.Helpers;
using RatingApp.Domain.Entities;
using RatingApp.Domain.Enums;

namespace RatingApp.Application.Tests.Services;

public class CandidateServiceTests
{
    private const double BaseLat = 40.7128;
    private const double BaseLon = -74.0060;

    private static (CandidateService svc, RatingApp.Infrastructure.Persistence.AppDbContext db) CreateService()
    {
        var db = InMemoryDbFactory.Create();
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApiBaseUrl"] = "http://localhost:5212" })
            .Build();
        var photoSvc = new PhotoService(db, envMock.Object, config);
        var svc = new CandidateService(db, NullLogger<CandidateService>.Instance, photoSvc);
        return (svc, db);
    }

    private static AppUser MakeUser(Gender gender, double lat, double lon,
        DateOnly? birthdate = null, Guid? id = null)
    {
        var userId = id ?? Guid.NewGuid();
        return new AppUser
        {
            Id = userId,
            Email = $"{userId}@test.com",
            DisplayName = "Test",
            Gender = gender,
            Birthdate = birthdate ?? new DateOnly(1995, 1, 1),
            Latitude = lat,
            Longitude = lon,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            Preference = new UserPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PreferredGender = gender == Gender.Man ? Gender.Woman : Gender.Man,
                MinAge = 18,
                MaxAge = 45,
                MaxDistanceMiles = 50
            }
        };
    }

    private static Photo MakePhoto(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        FileName = "photo.jpg",
        DisplayOrder = 0,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetCandidatesAsync_ReturnsOnlyPreferredGender()
    {
        var (svc, db) = CreateService();
        var male = MakeUser(Gender.Man, BaseLat, BaseLon);      // looking for women
        var female = MakeUser(Gender.Woman, BaseLat, BaseLon);  // should appear
        var anotherMale = MakeUser(Gender.Man, BaseLat, BaseLon); // should NOT appear
        db.Users.AddRange(male, female, anotherMale);
        db.Photos.Add(MakePhoto(female.Id));
        db.Photos.Add(MakePhoto(anotherMale.Id));
        await db.SaveChangesAsync();

        var results = await svc.GetCandidatesAsync(male.Id);

        results.Should().HaveCount(1);
        results[0].UserId.Should().Be(female.Id);
    }

    [Fact]
    public async Task GetCandidatesAsync_ExcludesCurrentUser()
    {
        var (svc, db) = CreateService();
        var me = MakeUser(Gender.Man, BaseLat, BaseLon);
        db.Users.Add(me);
        db.Photos.Add(MakePhoto(me.Id));
        await db.SaveChangesAsync();

        var results = await svc.GetCandidatesAsync(me.Id);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCandidatesAsync_ExcludesAlreadyRatedUsers()
    {
        var (svc, db) = CreateService();
        var me = MakeUser(Gender.Man, BaseLat, BaseLon);
        var her = MakeUser(Gender.Woman, BaseLat, BaseLon);
        db.Users.AddRange(me, her);
        db.Photos.Add(MakePhoto(her.Id));
        db.Ratings.Add(new Rating
        {
            Id = Guid.NewGuid(),
            RaterUserId = me.Id,
            RatedUserId = her.Id,
            Score = 8,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var results = await svc.GetCandidatesAsync(me.Id);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCandidatesAsync_ExcludesUsersWithoutPhotos()
    {
        var (svc, db) = CreateService();
        var me = MakeUser(Gender.Man, BaseLat, BaseLon);
        var her = MakeUser(Gender.Woman, BaseLat, BaseLon); // no photo added
        db.Users.AddRange(me, her);
        await db.SaveChangesAsync();

        var results = await svc.GetCandidatesAsync(me.Id);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCandidatesAsync_ExcludesCandidateOutsideAgeRange()
    {
        var (svc, db) = CreateService();
        var me = MakeUser(Gender.Man, BaseLat, BaseLon);
        me.Preference!.MinAge = 25;
        me.Preference!.MaxAge = 35;

        // Too young (age ~16)
        var tooYoung = MakeUser(Gender.Woman, BaseLat, BaseLon,
            birthdate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)));

        db.Users.AddRange(me, tooYoung);
        db.Photos.Add(MakePhoto(tooYoung.Id));
        await db.SaveChangesAsync();

        var results = await svc.GetCandidatesAsync(me.Id);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCandidatesAsync_IncludesMatchingCandidateWithPhoto()
    {
        var (svc, db) = CreateService();
        var me = MakeUser(Gender.Man, BaseLat, BaseLon);
        var her = MakeUser(Gender.Woman, BaseLat, BaseLon);
        db.Users.AddRange(me, her);
        db.Photos.Add(MakePhoto(her.Id));
        await db.SaveChangesAsync();

        var results = await svc.GetCandidatesAsync(me.Id);

        results.Should().HaveCount(1);
        results[0].PhotoUrls.Should().HaveCount(1);
    }
}
