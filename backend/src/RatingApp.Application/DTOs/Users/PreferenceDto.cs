namespace RatingApp.Application.DTOs.Users;

public record PreferenceDto(int PreferredGender, int MinAge, int MaxAge, double MaxDistanceMiles);
