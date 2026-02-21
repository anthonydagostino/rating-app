using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RatingApp.Application.DTOs.Ratings;
using RatingApp.Application.Services;
using RatingApp.Application.Tests.Helpers;
using RatingApp.Domain.Entities;
using RatingApp.Domain.Enums;

namespace RatingApp.Application.Tests.Services;

public class RatingServiceTests
{
    private static RatingService CreateService() =>
        new(InMemoryDbFactory.Create(), NullLogger<RatingService>.Instance);

    private static AppUser MakeUser(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = $"{Guid.NewGuid()}@test.com",
        DisplayName = "User",
        Gender = Gender.Man,
        Birthdate = new DateOnly(1995, 1, 1),
        PasswordHash = "hash",
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task SubmitRatingAsync_BothRateAboveThreshold_CreatesMatch()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var alice = MakeUser();
        var bob = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        // Alice rates Bob 8
        await svc.SubmitRatingAsync(alice.Id, new SubmitRatingRequest(bob.Id, 8));

        // Bob rates Alice 9 — should trigger match
        var matchId = await svc.SubmitRatingAsync(bob.Id, new SubmitRatingRequest(alice.Id, 9));

        matchId.Should().NotBeNull();
        db.Matches.Should().HaveCount(1);
        db.Chats.Should().HaveCount(1);
    }

    [Fact]
    public async Task SubmitRatingAsync_OneRateBelowThreshold_NoMatch()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var alice = MakeUser();
        var bob = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        await svc.SubmitRatingAsync(alice.Id, new SubmitRatingRequest(bob.Id, 5)); // below threshold
        var matchId = await svc.SubmitRatingAsync(bob.Id, new SubmitRatingRequest(alice.Id, 9));

        matchId.Should().BeNull();
        db.Matches.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitRatingAsync_BothBelowThreshold_NoMatch()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var alice = MakeUser();
        var bob = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        await svc.SubmitRatingAsync(alice.Id, new SubmitRatingRequest(bob.Id, 4));
        var matchId = await svc.SubmitRatingAsync(bob.Id, new SubmitRatingRequest(alice.Id, 3));

        matchId.Should().BeNull();
    }

    [Fact]
    public async Task SubmitRatingAsync_ThresholdExactly7_CreatesMatch()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var alice = MakeUser();
        var bob = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        await svc.SubmitRatingAsync(alice.Id, new SubmitRatingRequest(bob.Id, 7));
        var matchId = await svc.SubmitRatingAsync(bob.Id, new SubmitRatingRequest(alice.Id, 7));

        matchId.Should().NotBeNull("score of exactly 7 should qualify for a match");
    }

    [Fact]
    public async Task SubmitRatingAsync_UpdatingRating_DoesNotDuplicate()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var alice = MakeUser();
        var bob = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        await svc.SubmitRatingAsync(alice.Id, new SubmitRatingRequest(bob.Id, 5));
        await svc.SubmitRatingAsync(alice.Id, new SubmitRatingRequest(bob.Id, 8)); // update

        db.Ratings.Where(r => r.RaterUserId == alice.Id && r.RatedUserId == bob.Id)
            .Should().HaveCount(1);
        db.Ratings.Single(r => r.RaterUserId == alice.Id).Score.Should().Be(8);
    }

    [Fact]
    public async Task SubmitRatingAsync_RateSelf_ThrowsInvalidOperationException()
    {
        var svc = CreateService();
        var userId = Guid.NewGuid();

        var act = () => svc.SubmitRatingAsync(userId, new SubmitRatingRequest(userId, 8));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yourself*");
    }

    [Fact]
    public async Task GetRatingSummaryAsync_NoRatings_ReturnsZero()
    {
        var svc = CreateService();
        var userId = Guid.NewGuid();

        var summary = await svc.GetRatingSummaryAsync(userId);

        summary.AverageScore.Should().Be(0);
        summary.RatingCount.Should().Be(0);
    }

    [Fact]
    public async Task GetRatingSummaryAsync_MultipleRatings_ReturnsCorrectAverage()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var userId = Guid.NewGuid();
        db.Ratings.AddRange(
            new Rating { Id = Guid.NewGuid(), RaterUserId = Guid.NewGuid(), RatedUserId = userId, Score = 8, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Rating { Id = Guid.NewGuid(), RaterUserId = Guid.NewGuid(), RatedUserId = userId, Score = 6, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var summary = await svc.GetRatingSummaryAsync(userId);

        summary.AverageScore.Should().Be(7.0);
        summary.RatingCount.Should().Be(2);
    }
}
