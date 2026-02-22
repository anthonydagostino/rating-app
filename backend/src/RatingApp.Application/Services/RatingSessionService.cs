using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RatingApp.Application.DTOs.Sessions;
using RatingApp.Domain.Entities;
using RatingApp.Domain.Enums;
using RatingApp.Infrastructure.Persistence;

namespace RatingApp.Application.Services;

public class RatingSessionService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RatingSessionService> _logger;

    public RatingSessionService(AppDbContext db, ILogger<RatingSessionService> logger)
        => (_db, _logger) = (db, logger);

    public async Task<SessionDto> CreateSessionAsync(Guid creatorId, CreateSessionRequest request)
    {
        var candidate = await _db.Users.FindAsync(request.CandidateId)
            ?? throw new KeyNotFoundException($"Candidate {request.CandidateId} not found.");

        var creator = await _db.Users.FindAsync(creatorId)
            ?? throw new KeyNotFoundException($"Creator {creatorId} not found.");

        var session = new RatingSession
        {
            Id = Guid.NewGuid(),
            CandidateId = request.CandidateId,
            CreatorId = creatorId,
            Title = request.Title,
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        // Creator automatically joins as a participant
        session.ParticipantRatings.Add(new SessionParticipantRating
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            RaterUserId = creatorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _db.RatingSessions.Add(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Session {SessionId} created by {CreatorId} for candidate {CandidateId}",
            session.Id, creatorId, request.CandidateId);

        return MapToDto(session, candidate, creator);
    }

    public async Task<SessionDto> GetSessionAsync(Guid sessionId, Guid requestingUserId)
    {
        var session = await LoadSessionAsync(sessionId);
        EnsureParticipant(session, requestingUserId);
        return MapToDto(session, session.Candidate, session.Creator);
    }

    public async Task<SessionStateDto> GetSessionStateAsync(Guid sessionId, Guid requestingUserId)
    {
        var session = await LoadSessionAsync(sessionId);
        EnsureParticipant(session, requestingUserId);

        var recentMessages = session.Messages
            .OrderByDescending(m => m.CreatedAt)
            .Take(50)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new SessionMessageDto(
                m.Id,
                m.SenderUserId,
                m.Sender.DisplayName,
                m.Content,
                m.CreatedAt))
            .ToList();

        return new SessionStateDto(
            session.Id,
            session.Status.ToString(),
            MapRatings(session),
            recentMessages);
    }

    public async Task JoinSessionAsync(Guid sessionId, Guid userId)
    {
        var session = await LoadSessionAsync(sessionId);

        if (session.Status != SessionStatus.Active)
            throw new InvalidOperationException("Cannot join a session that is not active.");

        var alreadyJoined = session.ParticipantRatings.Any(r => r.RaterUserId == userId);
        if (alreadyJoined) return;

        session.ParticipantRatings.Add(new SessionParticipantRating
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            RaterUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("User {UserId} joined session {SessionId}", userId, sessionId);
    }

    public async Task LeaveSessionAsync(Guid sessionId, Guid userId)
    {
        var session = await _db.RatingSessions
            .Include(s => s.ParticipantRatings)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        var participation = session.ParticipantRatings.FirstOrDefault(r => r.RaterUserId == userId);
        if (participation is null) return;

        // Only remove if no rating has been submitted yet
        if (participation.Score is null)
        {
            _db.SessionParticipantRatings.Remove(participation);
            await _db.SaveChangesAsync();
        }

        _logger.LogInformation("User {UserId} left session {SessionId}", userId, sessionId);
    }

    public async Task<ParticipantRatingDto> SubmitRatingAsync(
        Guid sessionId, Guid raterId, SubmitSessionRatingRequest request)
    {
        if (request.Score is < 1 or > 10)
            throw new ArgumentException("Score must be between 1 and 10.");

        var session = await LoadSessionAsync(sessionId);

        if (session.Status != SessionStatus.Active)
            throw new InvalidOperationException("Cannot submit ratings to a non-active session.");

        var participation = session.ParticipantRatings.FirstOrDefault(r => r.RaterUserId == raterId)
            ?? throw new UnauthorizedAccessException("You are not a participant of this session.");

        if (participation.Score is not null)
            throw new InvalidOperationException("Rating already submitted. Use UpdateRating to change it.");

        participation.Score = request.Score;
        participation.Notes = request.Notes;
        participation.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("User {RaterId} submitted rating {Score} in session {SessionId}",
            raterId, request.Score, sessionId);

        return new ParticipantRatingDto(
            raterId,
            participation.Rater.DisplayName,
            participation.Score,
            participation.Notes,
            participation.UpdatedAt);
    }

    public async Task<ParticipantRatingDto> UpdateRatingAsync(
        Guid sessionId, Guid raterId, UpdateSessionRatingRequest request)
    {
        if (request.Score is < 1 or > 10)
            throw new ArgumentException("Score must be between 1 and 10.");

        var session = await LoadSessionAsync(sessionId);

        if (session.Status == SessionStatus.Finalized)
            throw new InvalidOperationException("Cannot edit ratings in a finalized session.");

        var participation = session.ParticipantRatings.FirstOrDefault(r => r.RaterUserId == raterId)
            ?? throw new UnauthorizedAccessException("You are not a participant of this session.");

        participation.Score = request.Score;
        participation.Notes = request.Notes;
        participation.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("User {RaterId} updated rating to {Score} in session {SessionId}",
            raterId, request.Score, sessionId);

        return new ParticipantRatingDto(
            raterId,
            participation.Rater.DisplayName,
            participation.Score,
            participation.Notes,
            participation.UpdatedAt);
    }

    public async Task<SessionMessageDto> SendMessageAsync(Guid sessionId, Guid senderId, string content)
    {
        var session = await LoadSessionAsync(sessionId);
        EnsureParticipant(session, senderId);

        if (session.Status == SessionStatus.Finalized)
            throw new InvalidOperationException("Cannot send messages in a finalized session.");

        var sender = await _db.Users.FindAsync(senderId)
            ?? throw new KeyNotFoundException($"User {senderId} not found.");

        var message = new SessionMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SenderUserId = senderId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _db.SessionMessages.Add(message);
        await _db.SaveChangesAsync();

        return new SessionMessageDto(message.Id, senderId, sender.DisplayName, content, message.CreatedAt);
    }

    public async Task LockSessionAsync(Guid sessionId, Guid requestingUserId)
    {
        var session = await LoadSessionAsync(sessionId);
        EnsureCreator(session, requestingUserId);

        if (session.Status != SessionStatus.Active)
            throw new InvalidOperationException("Only active sessions can be locked.");

        session.Status = SessionStatus.Locked;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Session {SessionId} locked by {UserId}", sessionId, requestingUserId);
    }

    public async Task FinalizeSessionAsync(Guid sessionId, Guid requestingUserId)
    {
        var session = await LoadSessionAsync(sessionId);
        EnsureCreator(session, requestingUserId);

        if (session.Status == SessionStatus.Finalized)
            throw new InvalidOperationException("Session is already finalized.");

        // Persist aggregated rating to the main Ratings store
        var submittedRatings = session.ParticipantRatings
            .Where(r => r.Score.HasValue)
            .ToList();

        if (submittedRatings.Count > 0)
        {
            var avgScore = (int)Math.Round(submittedRatings.Average(r => r.Score!.Value));

            // Use the creator as the rater for the aggregated rating
            var existing = await _db.Ratings
                .FirstOrDefaultAsync(r =>
                    r.RaterUserId == session.CreatorId &&
                    r.RatedUserId == session.CandidateId);

            if (existing is not null)
            {
                existing.Score = avgScore;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.Ratings.Add(new Rating
                {
                    Id = Guid.NewGuid(),
                    RaterUserId = session.CreatorId,
                    RatedUserId = session.CandidateId,
                    Score = avgScore,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            _logger.LogInformation(
                "Session {SessionId} finalized: aggregated score {Score} persisted for candidate {CandidateId}",
                sessionId, avgScore, session.CandidateId);
        }

        session.Status = SessionStatus.Finalized;
        session.FinalizedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // --- Helpers ---

    private async Task<RatingSession> LoadSessionAsync(Guid sessionId) =>
        await _db.RatingSessions
            .Include(s => s.Candidate)
            .Include(s => s.Creator)
            .Include(s => s.ParticipantRatings)
                .ThenInclude(r => r.Rater)
            .Include(s => s.Messages)
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
        ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

    private static void EnsureParticipant(RatingSession session, Guid userId)
    {
        if (!session.ParticipantRatings.Any(r => r.RaterUserId == userId))
            throw new UnauthorizedAccessException("You are not a participant of this session.");
    }

    private static void EnsureCreator(RatingSession session, Guid userId)
    {
        if (session.CreatorId != userId)
            throw new UnauthorizedAccessException("Only the session creator can perform this action.");
    }

    private static SessionDto MapToDto(RatingSession session, AppUser candidate, AppUser creator) =>
        new(
            session.Id,
            session.CandidateId,
            candidate.DisplayName,
            session.CreatorId,
            creator.DisplayName,
            session.Title,
            session.Status.ToString(),
            session.CreatedAt,
            session.FinalizedAt,
            MapRatings(session));

    private static List<ParticipantRatingDto> MapRatings(RatingSession session) =>
        session.ParticipantRatings
            .Select(r => new ParticipantRatingDto(
                r.RaterUserId,
                r.Rater.DisplayName,
                r.Score,
                r.Notes,
                r.UpdatedAt))
            .ToList();
}