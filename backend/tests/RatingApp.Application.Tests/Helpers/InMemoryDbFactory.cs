using Microsoft.EntityFrameworkCore;
using RatingApp.Infrastructure.Persistence;

namespace RatingApp.Application.Tests.Helpers;

public static class InMemoryDbFactory
{
    public static AppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
