using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RatingApp.Application.DTOs.Ratings;
using RatingApp.Domain.Entities;
using RatingApp.Infrastructure.Persistence;

namespace RatingApp.Application.Services;

public class RatingService : IRatingService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RatingService> _logger;
    private const int MatchThreshold = 7;

    public RatingService(AppDbContext db, ILogger<RatingService> logger)
        => (_db, _logger) = (db, logger);

    public async Task<Guid?> AddRating(Guid raterId, RatingCreateDto dto)
    {
        if (raterId == dto.RatedUserId)
            throw new InvalidOperationException("Cannot rate yourself.");

        var criteria = await _db.RatingCriteria
            .Where(c => c.IsActive)
            .ToListAsync();

        var requiredIds = criteria.Where(c => c.IsRequired).Select(c => c.Id).ToHashSet();
        var submittedIds = dto.Criteria.Select(c => c.CriterionId).ToHashSet();
        var missing = requiredIds.Except(submittedIds).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException("One or more required criteria are missing.");

        var totalWeight = criteria
            .Where(c => submittedIds.Contains(c.Id))
            .Sum(c => c.Weight);

        double weightedAggregate = totalWeight > 0
            ? dto.Criteria.Sum(cs =>
            {
                var criterion = criteria.FirstOrDefault(c => c.Id == cs.CriterionId);
                return criterion is not null ? cs.Score * criterion.Weight : 0;
            }) / totalWeight
            : 0;

        int aggregateScore = (int)Math.Round(weightedAggregate);

        var existing = await _db.Ratings
            .Include(r => r.RatingDetails)
            .FirstOrDefaultAsync(r => r.RaterUserId == raterId && r.RatedUserId == dto.RatedUserId);

        if (existing is not null)
        {
            existing.Score = aggregateScore;
            existing.Comment = dto.Comment;
            existing.UpdatedAt = DateTime.UtcNow;

            _db.RatingDetails.RemoveRange(existing.RatingDetails);
            foreach (var cs in dto.Criteria)
            {
                _db.RatingDetails.Add(new RatingDetail
                {
                    Id = Guid.NewGuid(),
                    RatingId = existing.Id,
                    CriterionId = cs.CriterionId,
                    Score = cs.Score
                });
            }
        }
        else
        {
            var rating = new Rating
            {
                Id = Guid.NewGuid(),
                RaterUserId = raterId,
                RatedUserId = dto.RatedUserId,
                Score = aggregateScore,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var cs in dto.Criteria)
            {
                rating.RatingDetails.Add(new RatingDetail
                {
                    Id = Guid.NewGuid(),
                    CriterionId = cs.CriterionId,
                    Score = cs.Score
                });
            }

            _db.Ratings.Add(rating);
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation(
            "Multi-criteria rating submitted: {RaterId} -> {RatedId} = {Score} (weighted)",
            raterId, dto.RatedUserId, aggregateScore);

        if (aggregateScore >= MatchThreshold)
        {
            var reverseRating = await _db.Ratings
                .FirstOrDefaultAsync(r =>
                    r.RaterUserId == dto.RatedUserId &&
                    r.RatedUserId == raterId &&
                    r.Score >= MatchThreshold);

            if (reverseRating is not null)
                return await CreateMatchAsync(raterId, dto.RatedUserId);
        }

        return null;
    }

    public async Task<AggregateRatingDto> GetAggregatedScores(Guid candidateId)
    {
        var details = await _db.RatingDetails
            .Include(rd => rd.Criterion)
            .Where(rd => rd.Rating.RatedUserId == candidateId)
            .ToListAsync();

        var totalRatings = await _db.Ratings
            .CountAsync(r => r.RatedUserId == candidateId);

        var criteria = await _db.RatingCriteria
            .Where(c => c.IsActive)
            .ToListAsync();

        var criteriaAverages = criteria.Select(c =>
        {
            var scores = details.Where(d => d.CriterionId == c.Id).Select(d => d.Score).ToList();
            double avg = scores.Count > 0 ? scores.Average() : 0;
            return new RatingDetailDto(c.Id, c.Name, Math.Round(avg, 2), c.Weight);
        }).ToList();

        var ratedCriteria = criteriaAverages.Where(ca => ca.AverageScore > 0).ToList();
        var totalWeight = ratedCriteria.Sum(ca => ca.Weight);
        double weightedAggregate = totalWeight > 0
            ? ratedCriteria.Sum(ca => ca.AverageScore * ca.Weight) / totalWeight
            : 0;

        return new AggregateRatingDto(criteriaAverages, Math.Round(weightedAggregate, 2), totalRatings);
    }

    public async Task<List<CriterionDto>> GetCriteriaAsync()
    {
        return await _db.RatingCriteria
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.Weight)
            .Select(c => new CriterionDto(c.Id, c.Name, c.Weight, c.IsRequired))
            .ToListAsync();
    }

    public async Task<RatingSummaryDto> GetRatingSummaryAsync(Guid userId)
    {
        var ratings = await _db.Ratings
            .Where(r => r.RatedUserId == userId)
            .ToListAsync();

        double avg = ratings.Count > 0 ? ratings.Average(r => r.Score) : 0;
        return new RatingSummaryDto(Math.Round(avg, 2), ratings.Count);
    }

    private async Task<Guid?> CreateMatchAsync(Guid userA, Guid userB)
    {
        // Normalize: smaller GUID is always UserAId
        if (userA.CompareTo(userB) > 0) (userA, userB) = (userB, userA);

        var existingMatch = await _db.Matches
            .FirstOrDefaultAsync(m => m.UserAId == userA && m.UserBId == userB);

        if (existingMatch is not null)
            return existingMatch.Id;

        var match = new Match
        {
            Id = Guid.NewGuid(),
            UserAId = userA,
            UserBId = userB,
            CreatedAt = DateTime.UtcNow,
            Chat = new Chat
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            }
        };

        _db.Matches.Add(match);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Match created: {UserA} <-> {UserB}, ChatId: {ChatId}", userA, userB, match.Chat.Id);
        return match.Id;
    }
}
