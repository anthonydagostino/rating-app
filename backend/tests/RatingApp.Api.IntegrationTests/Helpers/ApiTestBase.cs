using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using RatingApp.Domain.Entities;
using RatingApp.Domain.Enums;
using RatingApp.Infrastructure.Persistence;

namespace RatingApp.Api.IntegrationTests.Helpers;

public abstract class ApiTestBase : IDisposable
{
    protected static readonly Guid SkillCriterionId   = new("00000000-0000-0000-0000-000000000001");
    protected static readonly Guid CommCriterionId    = new("00000000-0000-0000-0000-000000000002");
    protected static readonly Guid CultureCriterionId = new("00000000-0000-0000-0000-000000000003");

    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected ApiTestBase()
    {
        Factory = new CustomWebApplicationFactory();
        Client = Factory.CreateClient();
        SeedCriteria();
    }

    private void SeedCriteria()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.RatingCriteria.Any()) return;
        db.RatingCriteria.AddRange(
            new RatingCriterion { Id = SkillCriterionId,   Name = "Skill",         Weight = 0.40, IsRequired = true,  IsActive = true },
            new RatingCriterion { Id = CommCriterionId,    Name = "Communication", Weight = 0.35, IsRequired = true,  IsActive = true },
            new RatingCriterion { Id = CultureCriterionId, Name = "Culture",       Weight = 0.25, IsRequired = false, IsActive = true }
        );
        db.SaveChanges();
    }

    /// <summary>
    /// Builds a multi-criteria rating payload scoring all three seeded criteria at
    /// the same <paramref name="score"/>. Replaces the old single-score format.
    /// </summary>
    protected object RatingPayload(Guid ratedUserId, int score) => new
    {
        ratedUserId,
        criteria = new[]
        {
            new { criterionId = SkillCriterionId,   score },
            new { criterionId = CommCriterionId,    score },
            new { criterionId = CultureCriterionId, score }
        },
        comment = (string?)null
    };

    protected HttpClient CreateAuthenticatedClient(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected async Task<(string token, Guid userId)> RegisterAndGetTokenAsync(
        string email = "test@test.com",
        string password = "Password123!",
        int gender = 1,
        double lat = 40.7128,
        double lon = -74.0060)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            displayName = "TestUser",
            gender,
            birthdate = "1995-06-15",
            latitude = lat,
            longitude = lon
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("No auth response");
        return (body.Token, body.UserId);
    }

    protected async Task SeedUserWithPhotoAsync(int gender = 2, double lat = 40.7128, double lon = -74.0060)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Email = $"{userId}@seed.com",
            DisplayName = "Seed User",
            Gender = (Gender)gender,
            Birthdate = new DateOnly(1995, 1, 1),
            Latitude = lat,
            Longitude = lon,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            Preference = new UserPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PreferredGender = gender == 1 ? Gender.Woman : Gender.Man,
                MinAge = 18,
                MaxAge = 45,
                MaxDistanceMiles = 50
            }
        };
        db.Users.Add(user);
        db.Photos.Add(new Photo
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileName = "seed.jpg",
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }

    private record AuthResponse(string Token, Guid UserId, string DisplayName, string Email);
}
