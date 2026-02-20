using RatingApp.Application.DTOs.Photos;

namespace RatingApp.Application.DTOs.Users;

public record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    int Gender,
    DateOnly Birthdate,
    double Latitude,
    double Longitude,
    int Age,
    IReadOnlyList<PhotoDto> Photos
);
