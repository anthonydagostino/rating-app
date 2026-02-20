namespace RatingApp.Domain.Entities;

public class Match
{
    public Guid Id { get; set; }
    public Guid UserAId { get; set; }
    public Guid UserBId { get; set; }
    public DateTime CreatedAt { get; set; }

    public AppUser UserA { get; set; } = null!;
    public AppUser UserB { get; set; } = null!;
    public Chat? Chat { get; set; }
}
