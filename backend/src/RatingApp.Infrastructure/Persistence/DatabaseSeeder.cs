using Bogus;
using Microsoft.EntityFrameworkCore;
using RatingApp.Domain.Entities;
using RatingApp.Domain.Enums;
using RatingApp.Domain.Interfaces;

namespace RatingApp.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher hasher)
    {
        if (await context.Users.AnyAsync())
            return;

        var faker = new Faker();
        var users = new List<AppUser>();

        for (int i = 0; i < 20; i++)
        {
            var gender = i % 2 == 0 ? Gender.Man : Gender.Woman;
            var minDob = DateTime.UtcNow.AddYears(-45);
            var maxDob = DateTime.UtcNow.AddYears(-18);
            var dob = DateOnly.FromDateTime(faker.Date.Between(minDob, maxDob));

            users.Add(new AppUser
            {
                Id = Guid.NewGuid(),
                Email = faker.Internet.Email().ToLowerInvariant(),
                DisplayName = faker.Name.FirstName(),
                Gender = gender,
                Birthdate = dob,
                Latitude = 40.7128 + faker.Random.Double(-1.5, 1.5),
                Longitude = -74.0060 + faker.Random.Double(-1.5, 1.5),
                PasswordHash = hasher.Hash("Password123!"),
                CreatedAt = DateTime.UtcNow.AddDays(-faker.Random.Int(0, 30)),
                Preference = new UserPreference
                {
                    Id = Guid.NewGuid(),
                    PreferredGender = gender == Gender.Man ? Gender.Woman : Gender.Man,
                    MinAge = 18,
                    MaxAge = 45,
                    MaxDistanceMiles = 50
                }
            });
        }

        context.Users.AddRange(users);
        await context.SaveChangesAsync();
    }
}
