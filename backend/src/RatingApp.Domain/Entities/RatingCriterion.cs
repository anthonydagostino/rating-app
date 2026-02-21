namespace RatingApp.Domain.Entities;

public class RatingCriterion
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }

    public ICollection<RatingDetail> RatingDetails { get; set; } = new List<RatingDetail>();
}
