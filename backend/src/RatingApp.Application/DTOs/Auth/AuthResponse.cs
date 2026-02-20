namespace RatingApp.Application.DTOs.Auth;

public record AuthResponse(string Token, Guid UserId, string DisplayName, string Email);
