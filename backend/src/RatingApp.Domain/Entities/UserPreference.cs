using RatingApp.Domain.Enums;

namespace RatingApp.Domain.Entities;

public class UserPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Gender PreferredGender { get; set; }
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public double MaxDistanceMiles { get; set; }

    public AppUser User { get; set; } = null!;
}
