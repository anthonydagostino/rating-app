namespace RatingApp.Domain.Entities;

public class RatingDetail
{
    public Guid Id { get; set; }
    public Guid RatingId { get; set; }
    public Guid CriterionId { get; set; }
    public int Score { get; set; }

    public Rating Rating { get; set; } = null!;
    public RatingCriterion Criterion { get; set; } = null!;
}
