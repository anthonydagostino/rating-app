namespace RatingApp.Domain.Entities;

public class SessionMessage
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public RatingSession Session { get; set; } = null!;
    public AppUser Sender { get; set; } = null!;
}