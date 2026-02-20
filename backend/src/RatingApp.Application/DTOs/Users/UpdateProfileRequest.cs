namespace RatingApp.Application.DTOs.Users;

public record UpdateProfileRequest(string DisplayName, double Latitude, double Longitude);
