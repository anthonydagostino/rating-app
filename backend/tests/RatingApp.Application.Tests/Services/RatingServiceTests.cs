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

    // ── SubmitRatingAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitRating_ScoreAboveThreshold_BothWays_CreatesMatch()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var alice = MakeUser();
        var bob = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        await svc.SubmitRatingAsync(alice.Id, new RateUserRequest(bob.Id, 8));
        var (matchCreated, matchId) = await svc.SubmitRatingAsync(bob.Id, new RateUserRequest(alice.Id, 9));

        matchCreated.Should().BeTrue();
        matchId.Should().NotBeNull();
        db.Matches.Should().HaveCount(1);
        db.Chats.Should().HaveCount(1);
    }

    [Fact]
    public async Task SubmitRating_OneScoreBelowThreshold_NoMatch()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var alice = MakeUser();
        var bob = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        await svc.SubmitRatingAsync(alice.Id, new RateUserRequest(bob.Id, 5)); // below threshold
        var (matchCreated, matchId) = await svc.SubmitRatingAsync(bob.Id, new RateUserRequest(alice.Id, 9));

        matchCreated.Should().BeFalse();
        matchId.Should().BeNull();
        db.Matches.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitRating_BothBelowThreshold_NoMatch()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var alice = MakeUser();
        var bob = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        await svc.SubmitRatingAsync(alice.Id, new RateUserRequest(bob.Id, 4));
        var (matchCreated, _) = await svc.SubmitRatingAsync(bob.Id, new RateUserRequest(alice.Id, 3));

        matchCreated.Should().BeFalse();
        db.Matches.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitRating_ExactlyThreshold7_CreatesMatch()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var alice = MakeUser();
        var bob = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        await svc.SubmitRatingAsync(alice.Id, new RateUserRequest(bob.Id, 7));
        var (matchCreated, matchId) = await svc.SubmitRatingAsync(bob.Id, new RateUserRequest(alice.Id, 7));

        matchCreated.Should().BeTrue("score of exactly 7 should qualify for a match");
        matchId.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitRating_UpdateExisting_DoesNotDuplicate()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var alice = MakeUser();
        var bob = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        await svc.SubmitRatingAsync(alice.Id, new RateUserRequest(bob.Id, 5));
        await svc.SubmitRatingAsync(alice.Id, new RateUserRequest(bob.Id, 8)); // update

        db.Ratings.Where(r => r.RaterUserId == alice.Id && r.RatedUserId == bob.Id)
            .Should().HaveCount(1);
        db.Ratings.Single(r => r.RaterUserId == alice.Id).Score.Should().Be(8);
    }

    [Fact]
    public async Task SubmitRating_Self_ThrowsInvalidOperationException()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var userId = Guid.NewGuid();

        Func<Task> act = () => svc.SubmitRatingAsync(userId, new RateUserRequest(userId, 8));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yourself*");
    }

    // ── GetRatingSummaryAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetRatingSummary_NoRatings_ReturnsZeroAndEmptyLabel()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var userId = Guid.NewGuid();

        var summary = await svc.GetRatingSummaryAsync(userId);

        summary.AverageScore.Should().Be(0);
        summary.RatingCount.Should().Be(0);
        summary.TopPercentLabel.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRatingSummary_MultipleRatings_ReturnsCorrectAverage()
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

    [Fact]
    public async Task GetRatingSummary_HighScores_ReturnsTopPercentLabel()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);

        // Seed 20 users with average scores of 5-6
        for (int i = 0; i < 20; i++)
        {
            var uid = Guid.NewGuid();
            db.Ratings.Add(new Rating { Id = Guid.NewGuid(), RaterUserId = Guid.NewGuid(), RatedUserId = uid, Score = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.Ratings.Add(new Rating { Id = Guid.NewGuid(), RaterUserId = Guid.NewGuid(), RatedUserId = uid, Score = 6, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        }

        // Target user has a high average of 9
        var targetUser = Guid.NewGuid();
        db.Ratings.Add(new Rating { Id = Guid.NewGuid(), RaterUserId = Guid.NewGuid(), RatedUserId = targetUser, Score = 9, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Ratings.Add(new Rating { Id = Guid.NewGuid(), RaterUserId = Guid.NewGuid(), RatedUserId = targetUser, Score = 9, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var summary = await svc.GetRatingSummaryAsync(targetUser);

        summary.TopPercentLabel.Should().NotBeEmpty("high-scoring user should receive a top percent label");
    }

    [Fact]
    public async Task GetRatingSummary_LessThan5Ratings_PercentileStillCalculated()
    {
        var db = InMemoryDbFactory.Create();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        var userId = Guid.NewGuid();
        db.Ratings.Add(new Rating { Id = Guid.NewGuid(), RaterUserId = Guid.NewGuid(), RatedUserId = userId, Score = 8, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        Func<Task> act = () => svc.GetRatingSummaryAsync(userId);

        await act.Should().NotThrowAsync("percentile calculation should work even with fewer than 5 ratings");
    }
}
