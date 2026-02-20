namespace RatingApp.Domain.Entities;

public class Chat
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Match Match { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
