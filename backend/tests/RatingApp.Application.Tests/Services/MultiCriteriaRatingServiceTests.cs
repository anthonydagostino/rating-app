using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RatingApp.Application.DTOs.Ratings;
using RatingApp.Application.Services;
using RatingApp.Application.Tests.Helpers;
using RatingApp.Domain.Entities;
using RatingApp.Domain.Enums;

namespace RatingApp.Application.Tests.Services;

public class MultiCriteriaRatingServiceTests
{
    private static readonly Guid SkillId   = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid CommId    = new("00000000-0000-0000-0000-000000000002");
    private static readonly Guid CultureId = new("00000000-0000-0000-0000-000000000003");

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

    private static async Task<(RatingService svc, AppUser alice, AppUser bob)> SetupAsync()
    {
        var db = InMemoryDbFactory.Create();

        db.RatingCriteria.AddRange(
            new RatingCriterion { Id = SkillId,   Name = "Skill",         Weight = 0.40, IsRequired = true,  IsActive = true },
            new RatingCriterion { Id = CommId,     Name = "Communication", Weight = 0.35, IsRequired = true,  IsActive = true },
            new RatingCriterion { Id = CultureId,  Name = "Culture",       Weight = 0.25, IsRequired = false, IsActive = true }
        );

        var alice = MakeUser();
        var bob   = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        var svc = new RatingService(db, NullLogger<RatingService>.Instance);
        return (svc, alice, bob);
    }

    private static RatingCreateDto MakeDto(Guid ratedUserId, int skill, int comm, int culture, string? comment = null) =>
        new(ratedUserId, new List<RatingCriterionScoreDto>
        {
            new(SkillId,   skill),
            new(CommId,    comm),
            new(CultureId, culture)
        }, comment);

    // --- AddRating / match threshold ---

    [Fact]
    public async Task AddRating_BothAboveThreshold_CreatesMatch()
    {
        var (svc, alice, bob) = await SetupAsync();

        // Weighted: skill=8*0.4 + comm=8*0.35 + culture=8*0.25 = 8.0
        await svc.AddRating(alice.Id, MakeDto(bob.Id, 8, 8, 8));
        var matchId = await svc.AddRating(bob.Id, MakeDto(alice.Id, 9, 9, 9));

        matchId.Should().NotBeNull("both weighted aggregates are >= 7");
    }

    [Fact]
    public async Task AddRating_AggregateBelow7_NoMatch()
    {
        var (svc, alice, bob) = await SetupAsync();

        // Alice: skill=4*0.4 + comm=4*0.35 + culture=4*0.25 = 4 → below threshold
        await svc.AddRating(alice.Id, MakeDto(bob.Id, 4, 4, 4));
        var matchId = await svc.AddRating(bob.Id, MakeDto(alice.Id, 9, 9, 9));

        matchId.Should().BeNull();
    }

    [Fact]
    public async Task AddRating_WeightedAggregateExactly7_CreatesMatch()
    {
        var (svc, alice, bob) = await SetupAsync();

        // skill=7, comm=7, culture=7 → aggregate = 7.0
        await svc.AddRating(alice.Id, MakeDto(bob.Id, 7, 7, 7));
        var matchId = await svc.AddRating(bob.Id, MakeDto(alice.Id, 7, 7, 7));

        matchId.Should().NotBeNull("aggregate of exactly 7 qualifies for match");
    }

    [Fact]
    public async Task AddRating_Self_ThrowsInvalidOperationException()
    {
        var (svc, alice, _) = await SetupAsync();

        var act = () => svc.AddRating(alice.Id, MakeDto(alice.Id, 8, 8, 8));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yourself*");
    }

    [Fact]
    public async Task AddRating_UpdateExisting_DoesNotDuplicateRecords()
    {
        var db = InMemoryDbFactory.Create();
        db.RatingCriteria.AddRange(
            new RatingCriterion { Id = SkillId,  Name = "Skill",         Weight = 0.40, IsRequired = true, IsActive = true },
            new RatingCriterion { Id = CommId,   Name = "Communication", Weight = 0.35, IsRequired = true, IsActive = true },
            new RatingCriterion { Id = CultureId, Name = "Culture",      Weight = 0.25, IsRequired = false, IsActive = true }
        );
        var alice = MakeUser();
        var bob   = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);

        await svc.AddRating(alice.Id, MakeDto(bob.Id, 5, 5, 5));
        await svc.AddRating(alice.Id, MakeDto(bob.Id, 9, 9, 9)); // update

        db.Ratings
            .Where(r => r.RaterUserId == alice.Id && r.RatedUserId == bob.Id)
            .Should().HaveCount(1);

        // After update, 3 detail records (old removed, new added)
        db.RatingDetails
            .Where(d => d.Rating.RaterUserId == alice.Id)
            .Should().HaveCount(3);
    }

    [Fact]
    public async Task AddRating_MissingRequiredCriterion_ThrowsInvalidOperationException()
    {
        var (svc, alice, bob) = await SetupAsync();

        // Only supply Skill — Communication is required but missing
        var dto = new RatingCreateDto(bob.Id, new List<RatingCriterionScoreDto>
        {
            new(SkillId, 8)
        }, null);

        var act = () => svc.AddRating(alice.Id, dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*required criteria*");
    }

    // --- GetAggregatedScores ---

    [Fact]
    public async Task GetAggregatedScores_NoRatings_ReturnsZeroAggregate()
    {
        var (svc, _, bob) = await SetupAsync();

        var result = await svc.GetAggregatedScores(bob.Id);

        result.WeightedAggregate.Should().Be(0);
        result.TotalRatings.Should().Be(0);
        result.CriteriaAverages.Should().AllSatisfy(d => d.AverageScore.Should().Be(0));
    }

    [Fact]
    public async Task GetAggregatedScores_MultipleRaters_ReturnsCorrectPerCriterionAverages()
    {
        var db = InMemoryDbFactory.Create();
        db.RatingCriteria.AddRange(
            new RatingCriterion { Id = SkillId,   Name = "Skill",         Weight = 0.40, IsRequired = true,  IsActive = true },
            new RatingCriterion { Id = CommId,     Name = "Communication", Weight = 0.35, IsRequired = true,  IsActive = true },
            new RatingCriterion { Id = CultureId,  Name = "Culture",       Weight = 0.25, IsRequired = false, IsActive = true }
        );
        var alice = MakeUser();
        var bob   = MakeUser();
        var carol = MakeUser();
        db.Users.AddRange(alice, bob, carol);
        await db.SaveChangesAsync();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);

        // Alice rates Bob: skill=8, comm=6, culture=10
        await svc.AddRating(alice.Id, MakeDto(bob.Id, 8, 6, 10));
        // Carol rates Bob: skill=6, comm=8, culture=6
        await svc.AddRating(carol.Id, MakeDto(bob.Id, 6, 8, 6));

        var result = await svc.GetAggregatedScores(bob.Id);

        result.TotalRatings.Should().Be(2);

        var skill = result.CriteriaAverages.Single(c => c.CriterionId == SkillId);
        skill.AverageScore.Should().Be(7.0); // (8+6)/2

        var comm = result.CriteriaAverages.Single(c => c.CriterionId == CommId);
        comm.AverageScore.Should().Be(7.0); // (6+8)/2

        var culture = result.CriteriaAverages.Single(c => c.CriterionId == CultureId);
        culture.AverageScore.Should().Be(8.0); // (10+6)/2

        // Weighted aggregate = (7*0.4 + 7*0.35 + 8*0.25) / 1.0 = 2.8 + 2.45 + 2.0 = 7.25
        result.WeightedAggregate.Should().Be(7.25);
    }

    [Fact]
    public async Task GetAggregatedScores_MatchesBackendWeightedComputation()
    {
        var (svc, alice, bob) = await SetupAsync();

        // skill=10, comm=5, culture=8
        // aggregate = (10*0.4 + 5*0.35 + 8*0.25) / 1.0 = 4.0 + 1.75 + 2.0 = 7.75 → rounded = 8
        await svc.AddRating(alice.Id, MakeDto(bob.Id, 10, 5, 8));

        var aggregate = await svc.GetAggregatedScores(bob.Id);

        aggregate.WeightedAggregate.Should().Be(7.75);
    }

    // --- GetRatingSummaryAsync ---

    [Fact]
    public async Task GetRatingSummaryAsync_ReturnsAverageAggregateScore()
    {
        var (svc, alice, bob) = await SetupAsync();

        // skill=8, comm=8, culture=8 → aggregate = 8
        await svc.AddRating(alice.Id, MakeDto(bob.Id, 8, 8, 8));

        var summary = await svc.GetRatingSummaryAsync(bob.Id);

        summary.RatingCount.Should().Be(1);
        summary.AverageScore.Should().Be(8.0);
    }

    // --- Rounding boundary ---

    [Fact]
    public async Task AddRating_AggregateBankersRoundsTo6_NoMatch()
    {
        var (svc, alice, bob) = await SetupAsync();

        // 6*0.40 + 6*0.35 + 8*0.25 = 2.40 + 2.10 + 2.00 = 6.5
        // Math.Round(6.5) = 6 (banker's rounding → rounds to even) → below threshold
        await svc.AddRating(alice.Id, MakeDto(bob.Id, 6, 6, 8));
        var matchId = await svc.AddRating(bob.Id, MakeDto(alice.Id, 9, 9, 9));

        matchId.Should().BeNull("aggregate of 6.5 rounds to 6 via banker's rounding, below match threshold");
    }

    [Fact]
    public async Task AddRating_AggregateRoundsUpTo7_CreatesMatch()
    {
        var (svc, alice, bob) = await SetupAsync();

        // 6*0.40 + 7*0.35 + 7*0.25 = 2.40 + 2.45 + 1.75 = 6.6
        // Math.Round(6.6) = 7 → at threshold → match
        await svc.AddRating(alice.Id, MakeDto(bob.Id, 6, 7, 7));
        var matchId = await svc.AddRating(bob.Id, MakeDto(alice.Id, 9, 9, 9));

        matchId.Should().NotBeNull("aggregate of 6.6 rounds to 7, which meets the match threshold");
    }

    // --- Rating updates ---

    [Fact]
    public async Task AddRating_UpdateFromBelowToAboveThreshold_CreatesMatch()
    {
        var (svc, alice, bob) = await SetupAsync();

        // Alice rates Bob below threshold; Bob rates Alice above → no match yet
        await svc.AddRating(alice.Id, MakeDto(bob.Id, 4, 4, 4));
        await svc.AddRating(bob.Id, MakeDto(alice.Id, 9, 9, 9));

        // Alice re-rates Bob above threshold → both sides now qualify → match
        var matchId = await svc.AddRating(alice.Id, MakeDto(bob.Id, 8, 8, 8));

        matchId.Should().NotBeNull("updating Alice's rating above threshold should trigger a match");
    }

    [Fact]
    public async Task AddRating_MatchAlreadyExists_ReturnsExistingMatchId()
    {
        var (svc, alice, bob) = await SetupAsync();

        await svc.AddRating(alice.Id, MakeDto(bob.Id, 8, 8, 8));
        var firstMatchId = await svc.AddRating(bob.Id, MakeDto(alice.Id, 9, 9, 9));

        // Alice re-rates — match already exists; no duplicate should be created
        var secondMatchId = await svc.AddRating(alice.Id, MakeDto(bob.Id, 10, 10, 10));

        firstMatchId.Should().NotBeNull();
        secondMatchId.Should().Be(firstMatchId, "re-rating when a match exists should return the existing match ID");
    }

    // --- Optional criteria ---

    [Fact]
    public async Task AddRating_OptionalCriterionOmitted_Succeeds()
    {
        var (svc, alice, bob) = await SetupAsync();

        // Skill + Communication are required; Culture is optional and omitted here
        var dto = new RatingCreateDto(bob.Id, new List<RatingCriterionScoreDto>
        {
            new(SkillId, 8),
            new(CommId,  8)
        }, null);

        var act = () => svc.AddRating(alice.Id, dto);

        await act.Should().NotThrowAsync("optional criteria may be omitted from a submission");
    }

    // --- Comment persistence ---

    [Fact]
    public async Task AddRating_CommentStoredOnNewRating()
    {
        var db = InMemoryDbFactory.Create();
        db.RatingCriteria.AddRange(
            new RatingCriterion { Id = SkillId,   Name = "Skill",         Weight = 0.40, IsRequired = true,  IsActive = true },
            new RatingCriterion { Id = CommId,     Name = "Communication", Weight = 0.35, IsRequired = true,  IsActive = true },
            new RatingCriterion { Id = CultureId,  Name = "Culture",       Weight = 0.25, IsRequired = false, IsActive = true }
        );
        var alice = MakeUser();
        var bob   = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);

        await svc.AddRating(alice.Id, MakeDto(bob.Id, 8, 8, 8, comment: "Great person!"));

        var rating = db.Ratings.Single(r => r.RaterUserId == alice.Id);
        rating.Comment.Should().Be("Great person!");
    }

    [Fact]
    public async Task AddRating_CommentUpdatedOnResubmit()
    {
        var db = InMemoryDbFactory.Create();
        db.RatingCriteria.AddRange(
            new RatingCriterion { Id = SkillId,   Name = "Skill",         Weight = 0.40, IsRequired = true,  IsActive = true },
            new RatingCriterion { Id = CommId,     Name = "Communication", Weight = 0.35, IsRequired = true,  IsActive = true },
            new RatingCriterion { Id = CultureId,  Name = "Culture",       Weight = 0.25, IsRequired = false, IsActive = true }
        );
        var alice = MakeUser();
        var bob   = MakeUser();
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);

        await svc.AddRating(alice.Id, MakeDto(bob.Id, 8, 8, 8, comment: "Initial comment"));
        await svc.AddRating(alice.Id, MakeDto(bob.Id, 9, 9, 9, comment: "Updated comment"));

        var rating = db.Ratings.Single(r => r.RaterUserId == alice.Id);
        rating.Comment.Should().Be("Updated comment");
    }

    // --- GetCriteriaAsync ---

    [Fact]
    public async Task GetCriteriaAsync_ReturnsOnlyActiveCriteria()
    {
        var (svc, _, _) = await SetupAsync();

        var criteria = await svc.GetCriteriaAsync();

        criteria.Should().HaveCount(3);
        criteria.Should().AllSatisfy(c => c.Id.Should().NotBeEmpty());
    }

    [Fact]
    public async Task GetCriteriaAsync_InactiveCriterionExcluded()
    {
        var db = InMemoryDbFactory.Create();
        var inactiveId = Guid.NewGuid();
        db.RatingCriteria.AddRange(
            new RatingCriterion { Id = SkillId,    Name = "Skill",         Weight = 0.40, IsRequired = true,  IsActive = true  },
            new RatingCriterion { Id = CommId,     Name = "Communication", Weight = 0.35, IsRequired = true,  IsActive = true  },
            new RatingCriterion { Id = CultureId,  Name = "Culture",       Weight = 0.25, IsRequired = false, IsActive = true  },
            new RatingCriterion { Id = inactiveId, Name = "Hidden",        Weight = 0.10, IsRequired = false, IsActive = false }
        );
        await db.SaveChangesAsync();
        var svc = new RatingService(db, NullLogger<RatingService>.Instance);

        var criteria = await svc.GetCriteriaAsync();

        criteria.Should().HaveCount(3, "inactive criteria must not be returned");
        criteria.Should().NotContain(c => c.Id == inactiveId);
    }

    [Fact]
    public async Task GetCriteriaAsync_ReturnedInWeightDescendingOrder()
    {
        var (svc, _, _) = await SetupAsync();

        var criteria = await svc.GetCriteriaAsync();

        // Expected order: Skill (0.40) → Communication (0.35) → Culture (0.25)
        criteria.Select(c => c.Weight).Should().BeInDescendingOrder();
    }
}