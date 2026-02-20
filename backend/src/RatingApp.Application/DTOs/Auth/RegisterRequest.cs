namespace RatingApp.Application.DTOs.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    int Gender,
    DateOnly Birthdate,
    double Latitude,
    double Longitude
);
