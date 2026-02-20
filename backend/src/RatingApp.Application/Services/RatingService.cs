using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RatingApp.Application.DTOs.Ratings;
using RatingApp.Domain.Entities;
using RatingApp.Infrastructure.Persistence;

namespace RatingApp.Application.Services;

public class RatingService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RatingService> _logger;
    private const int MatchThreshold = 7;

    public RatingService(AppDbContext db, ILogger<RatingService> logger)
        => (_db, _logger) = (db, logger);

    public async Task<Guid?> SubmitRatingAsync(Guid raterId, SubmitRatingRequest req)
    {
        if (raterId == req.RatedUserId)
            throw new InvalidOperationException("Cannot rate yourself.");

        var existing = await _db.Ratings
            .FirstOrDefaultAsync(r => r.RaterUserId == raterId && r.RatedUserId == req.RatedUserId);

        if (existing is not null)
        {
            existing.Score = req.Score;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.Ratings.Add(new Rating
            {
                Id = Guid.NewGuid(),
                RaterUserId = raterId,
                RatedUserId = req.RatedUserId,
                Score = req.Score,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Rating submitted: {RaterId} -> {RatedId} = {Score}", raterId, req.RatedUserId, req.Score);

        if (req.Score >= MatchThreshold)
        {
            var reverseRating = await _db.Ratings
                .FirstOrDefaultAsync(r =>
                    r.RaterUserId == req.RatedUserId &&
                    r.RatedUserId == raterId &&
                    r.Score >= MatchThreshold);

            if (reverseRating is not null)
                return await CreateMatchAsync(raterId, req.RatedUserId);
        }

        return null;
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

    public async Task<RatingSummaryDto> GetRatingSummaryAsync(Guid userId)
    {
        var ratings = await _db.Ratings
            .Where(r => r.RatedUserId == userId)
            .ToListAsync();

        double avg = ratings.Count > 0 ? ratings.Average(r => r.Score) : 0;
        return new RatingSummaryDto(Math.Round(avg, 2), ratings.Count);
    }
}
