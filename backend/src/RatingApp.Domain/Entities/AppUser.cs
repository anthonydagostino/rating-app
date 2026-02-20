using RatingApp.Domain.Enums;

namespace RatingApp.Domain.Entities;

public class AppUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateOnly Birthdate { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public UserPreference? Preference { get; set; }
    public ICollection<Rating> RatingsGiven { get; set; } = new List<Rating>();
    public ICollection<Rating> RatingsReceived { get; set; } = new List<Rating>();
    public ICollection<Message> MessagesSent { get; set; } = new List<Message>();
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
