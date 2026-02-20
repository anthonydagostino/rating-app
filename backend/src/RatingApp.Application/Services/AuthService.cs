using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RatingApp.Application.DTOs.Auth;
using RatingApp.Domain.Entities;
using RatingApp.Domain.Enums;
using RatingApp.Domain.Interfaces;
using RatingApp.Infrastructure.Persistence;

namespace RatingApp.Application.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IPasswordHasher hasher, IJwtTokenGenerator jwt, ILogger<AuthService> logger)
        => (_db, _hasher, _jwt, _logger) = (db, hasher, jwt, logger);

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email.ToLowerInvariant()))
            throw new InvalidOperationException("Email already registered.");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = req.Email.ToLowerInvariant(),
            DisplayName = req.DisplayName,
            Gender = (Gender)req.Gender,
            Birthdate = req.Birthdate,
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            PasswordHash = _hasher.Hash(req.Password),
            CreatedAt = DateTime.UtcNow,
            Preference = new UserPreference
            {
                Id = Guid.NewGuid(),
                PreferredGender = (Gender)req.Gender == Gender.Man ? Gender.Woman : Gender.Man,
                MinAge = 18,
                MaxAge = 35,
                MaxDistanceMiles = 25
            }
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User registered: {Email}", user.Email);

        var token = _jwt.GenerateToken(user.Id, user.Email, user.DisplayName);
        return new AuthResponse(token, user.Id, user.DisplayName, user.Email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!_hasher.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var token = _jwt.GenerateToken(user.Id, user.Email, user.DisplayName);
        return new AuthResponse(token, user.Id, user.DisplayName, user.Email);
    }
}
