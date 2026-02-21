using RatingApp.Domain.Enums;

namespace RatingApp.Domain.Entities;

public class RatingSession
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Guid CreatorId { get; set; }
    public string? Title { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }

    public AppUser Candidate { get; set; } = null!;
    public AppUser Creator { get; set; } = null!;
    public ICollection<SessionParticipantRating> ParticipantRatings { get; set; } = new List<SessionParticipantRating>();
    public ICollection<SessionMessage> Messages { get; set; } = new List<SessionMessage>();
}