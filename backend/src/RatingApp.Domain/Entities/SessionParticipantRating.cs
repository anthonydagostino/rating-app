namespace RatingApp.Domain.Entities;

public class SessionParticipantRating
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid RaterUserId { get; set; }
    public int? Score { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public RatingSession Session { get; set; } = null!;
    public AppUser Rater { get; set; } = null!;
}