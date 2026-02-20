namespace RatingApp.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Chat Chat { get; set; } = null!;
    public AppUser Sender { get; set; } = null!;
}
