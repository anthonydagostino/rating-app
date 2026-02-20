namespace RatingApp.Domain.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, string displayName);
}
