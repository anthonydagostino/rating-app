using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using RatingApp.Application.Services;
using RatingApp.Application.Tests.Helpers;
using RatingApp.Domain.Entities;

namespace RatingApp.Application.Tests.Services;

public class PhotoServiceUploadTests
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

    private static Mock<IFormFile> MakeFileMock(string fileName = "photo.jpg", long length = 1024)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.Length).Returns(length);
        mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    // ── Max photos ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadPhotoAsync_AtMaxPhotos_ThrowsInvalidOperationException()
    {
        var (svc, db) = CreateService();
        var userId = Guid.NewGuid();

        // Seed 6 photos (the max)
        for (var i = 0; i < 6; i++)
        {
            db.Photos.Add(new Photo
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = $"photo{i}.jpg",
                DisplayOrder = i,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var act = () => svc.UploadPhotoAsync(userId, MakeFileMock().Object);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Maximum*6*");
    }

    // ── File extension ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.jpeg")]
    [InlineData("photo.png")]
    [InlineData("photo.webp")]
    [InlineData("photo.JPG")]   // case-insensitive
    public async Task UploadPhotoAsync_AllowedExtensions_DoNotThrowOnExtension(string fileName)
    {
        var (svc, _) = CreateService();
        var userId = Guid.NewGuid();

        // We expect it NOT to throw an extension error.
        // It may throw a file-system error after passing validation — that's fine.
        var act = () => svc.UploadPhotoAsync(userId, MakeFileMock(fileName).Object);

        // Should not throw an InvalidOperationException containing "JPG, PNG, or WebP"
        await act.Should().NotThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData("photo.gif")]
    [InlineData("photo.bmp")]
    [InlineData("photo.tiff")]
    [InlineData("photo.exe")]
    public async Task UploadPhotoAsync_DisallowedExtension_ThrowsInvalidOperationException(string fileName)
    {
        var (svc, _) = CreateService();

        var act = () => svc.UploadPhotoAsync(Guid.NewGuid(), MakeFileMock(fileName).Object);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*JPG, PNG, or WebP*");
    }

    // ── File size ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadPhotoAsync_FileTooLarge_ThrowsInvalidOperationException()
    {
        var (svc, _) = CreateService();
        var oversized = 5L * 1024 * 1024 + 1; // 5 MB + 1 byte

        var act = () => svc.UploadPhotoAsync(Guid.NewGuid(), MakeFileMock(length: oversized).Object);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*5 MB*");
    }

    [Fact]
    public async Task UploadPhotoAsync_ExactlyAtSizeLimit_DoesNotThrowSizeError()
    {
        var (svc, _) = CreateService();
        var exactLimit = 5L * 1024 * 1024; // exactly 5 MB

        var act = () => svc.UploadPhotoAsync(Guid.NewGuid(), MakeFileMock(length: exactLimit).Object);

        // Should not throw a size-related error (may throw file-system related error)
        await act.Should().NotThrowAsync<InvalidOperationException>();
    }

    // ── DisplayOrder assignment ────────────────────────────────────────────────

    [Fact]
    public async Task UploadPhotoAsync_SecondPhoto_AssignsIncrementalDisplayOrder()
    {
        var (svc, db) = CreateService();
        var userId = Guid.NewGuid();

        // Seed one photo so count == 1
        db.Photos.Add(new Photo
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileName = "existing.jpg",
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var dto = await svc.UploadPhotoAsync(userId, MakeFileMock().Object);

        dto.DisplayOrder.Should().Be(1);
    }
}
