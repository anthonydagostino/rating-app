namespace RatingApp.Domain.Entities;

public class Rating
{
    public Guid Id { get; set; }
    public Guid RaterUserId { get; set; }
    public Guid RatedUserId { get; set; }
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? Comment { get; set; }

    public AppUser Rater { get; set; } = null!;
    public AppUser Rated { get; set; } = null!;
    public ICollection<RatingDetail> RatingDetails { get; set; } = new List<RatingDetail>();
}
