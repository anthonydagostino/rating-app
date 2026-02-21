using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RatingApp.Application.Services;
using RatingApp.Application.Tests.Helpers;
using RatingApp.Domain.Entities;

namespace RatingApp.Application.Tests.Services;

public class PhotoServiceTests
{
    private static (PhotoService svc, RatingApp.Infrastructure.Persistence.AppDbContext db) CreateService()
    {
        var db = InMemoryDbFactory.Create();
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApiBaseUrl"] = "http://localhost:5212" })
            .Build();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        return (new PhotoService(db, envMock.Object, config, httpContextAccessor.Object), db);
    }

    [Fact]
    public void BuildUrl_ReturnsCorrectFormat()
    {
        var (svc, _) = CreateService();
        var userId = Guid.NewGuid();

        var url = svc.BuildUrl(userId, "photo.jpg");

        url.Should().Be($"http://localhost:5212/uploads/{userId}/photo.jpg");
    }

    [Fact]
    public async Task GetUserPhotosAsync_ReturnsOrderedByDisplayOrder()
    {
        var (svc, db) = CreateService();
        var userId = Guid.NewGuid();
        db.Photos.AddRange(
            new Photo { Id = Guid.NewGuid(), UserId = userId, FileName = "b.jpg", DisplayOrder = 1, CreatedAt = DateTime.UtcNow },
            new Photo { Id = Guid.NewGuid(), UserId = userId, FileName = "a.jpg", DisplayOrder = 0, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var photos = await svc.GetUserPhotosAsync(userId);

        photos.Should().HaveCount(2);
        photos[0].Url.Should().Contain("a.jpg");
        photos[1].Url.Should().Contain("b.jpg");
    }

    [Fact]
    public async Task DeletePhotoAsync_LastPhoto_ThrowsInvalidOperationException()
    {
        var (svc, db) = CreateService();
        var userId = Guid.NewGuid();
        var photoId = Guid.NewGuid();
        db.Photos.Add(new Photo
        {
            Id = photoId,
            UserId = userId,
            FileName = "only.jpg",
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var act = () => svc.DeletePhotoAsync(userId, photoId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*at least 1 photo*");
    }

    [Fact]
    public async Task GetUserPhotosAsync_EmptyForUserWithNoPhotos()
    {
        var (svc, _) = CreateService();

        var photos = await svc.GetUserPhotosAsync(Guid.NewGuid());

        photos.Should().BeEmpty();
    }
}
