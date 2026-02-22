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

    private static readonly int[] TopPercentBuckets = { 1, 5, 10, 15, 20, 25, 50 };

    public RatingService(AppDbContext db, ILogger<RatingService> logger)
        => (_db, _logger) = (db, logger);

    public async Task<(bool MatchCreated, Guid? MatchId)> SubmitRatingAsync(Guid raterId, RateUserRequest request)
    {
        if (raterId == request.RatedUserId)
            throw new InvalidOperationException("Cannot rate yourself.");

        var existing = await _db.Ratings
            .FirstOrDefaultAsync(r => r.RaterUserId == raterId && r.RatedUserId == request.RatedUserId);

        if (existing is not null)
        {
            existing.Score = request.Score;
            existing.Comment = request.Comment;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.Ratings.Add(new Rating
            {
                Id = Guid.NewGuid(),
                RaterUserId = raterId,
                RatedUserId = request.RatedUserId,
                Score = request.Score,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Rating submitted: {RaterId} -> {RatedId} = {Score}",
            raterId, request.RatedUserId, request.Score);

        if (request.Score >= MatchThreshold)
        {
            var reverseRating = await _db.Ratings
                .FirstOrDefaultAsync(r =>
                    r.RaterUserId == request.RatedUserId &&
                    r.RatedUserId == raterId &&
                    r.Score >= MatchThreshold);

            if (reverseRating is not null)
            {
                var matchId = await CreateMatchAsync(raterId, request.RatedUserId);
                return (true, matchId);
            }
        }

        return (false, null);
    }

    public async Task<RatingSummaryDto> GetRatingSummaryAsync(Guid userId)
    {
        var userRatings = await _db.Ratings
            .Where(r => r.RatedUserId == userId)
            .ToListAsync();

        if (userRatings.Count == 0)
            return new RatingSummaryDto(0, 0, 0, "");

        double userAvg = userRatings.Average(r => r.Score);

        var allAverages = await _db.Ratings
            .GroupBy(r => r.RatedUserId)
            .Select(g => g.Average(r => r.Score))
            .ToListAsync();

        int totalUsers = allAverages.Count;
        int lowerCount = allAverages.Count(avg => avg < userAvg);
        double percentile = (double)lowerCount / totalUsers * 100;

        string topPercentLabel = ComputeTopPercentLabel(percentile);

        return new RatingSummaryDto(Math.Round(userAvg, 2), userRatings.Count, Math.Round(percentile, 2), topPercentLabel);
    }

    private static string ComputeTopPercentLabel(double percentile)
    {
        int topPercent = 100 - (int)Math.Floor(percentile);
        int nearest = TopPercentBuckets.MinBy(b => Math.Abs(b - topPercent));
        return $"Top {nearest}%";
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
