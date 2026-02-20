namespace RatingApp.Client.Models;

public record PhotoDto(Guid Id, string Url, int DisplayOrder);

public record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    int Gender,
    DateOnly Birthdate,
    double Latitude,
    double Longitude,
    int Age,
    IReadOnlyList<PhotoDto>? Photos
);

public record UpdateProfileRequest(string DisplayName, double Latitude, double Longitude);

public record PreferenceDto(int PreferredGender, int MinAge, int MaxAge, double MaxDistanceMiles);

public record RatingSummaryDto(double AverageScore, int RatingCount);
