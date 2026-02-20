namespace RatingApp.Client.Models;

public record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    int Gender,
    DateOnly Birthdate,
    double Latitude,
    double Longitude
);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, Guid UserId, string DisplayName, string Email);
