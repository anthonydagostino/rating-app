using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RatingApp.Api.IntegrationTests.Helpers;
using RatingApp.Domain.Entities;
using RatingApp.Infrastructure.Persistence;

namespace RatingApp.Api.IntegrationTests;

public class PhotosIntegrationTests : ApiTestBase
{
    // ── GET /api/photos ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetPhotos_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/photos");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPhotos_Authenticated_ReturnsEmptyListInitially()
    {
        var (token, _) = await RegisterAndGetTokenAsync("photos-get@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.GetAsync("/api/photos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("[]");
    }

    // ── POST /api/photos ──────────────────────────────────────────────────────

    [Fact]
    public async Task UploadPhoto_Unauthenticated_Returns401()
    {
        using var content = BuildImageContent("test.jpg");
        var response = await Client.PostAsync("/api/photos", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadPhoto_ValidJpeg_Returns200WithPhotoDto()
    {
        var (token, _) = await RegisterAndGetTokenAsync("photos-upload@test.com");
        var authed = CreateAuthenticatedClient(token);

        using var content = BuildImageContent("photo.jpg");
        var response = await authed.PostAsync("/api/photos", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("url");
        body.Should().Contain("displayOrder");
    }

    [Fact]
    public async Task UploadPhoto_AfterUpload_AppearsInGetPhotos()
    {
        var (token, _) = await RegisterAndGetTokenAsync("photos-list@test.com");
        var authed = CreateAuthenticatedClient(token);

        using var uploadContent = BuildImageContent("photo.jpg");
        await authed.PostAsync("/api/photos", uploadContent);

        var listResponse = await authed.GetAsync("/api/photos");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await listResponse.Content.ReadAsStringAsync();
        body.Should().Contain("url");
    }

    [Fact]
    public async Task UploadPhoto_DisallowedExtension_Returns400()
    {
        var (token, _) = await RegisterAndGetTokenAsync("photos-badext@test.com");
        var authed = CreateAuthenticatedClient(token);

        using var content = BuildImageContent("virus.exe");
        var response = await authed.PostAsync("/api/photos", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("JPG, PNG, or WebP");
    }

    // ── DELETE /api/photos/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task DeletePhoto_Unauthenticated_Returns401()
    {
        var response = await Client.DeleteAsync($"/api/photos/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePhoto_LastPhoto_Returns400()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("photos-delete-last@test.com");

        // Seed exactly one photo directly into DB
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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

        var authed = CreateAuthenticatedClient(token);
        var response = await authed.DeleteAsync($"/api/photos/{photoId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("at least 1 photo");
    }

    [Fact]
    public async Task DeletePhoto_NonExistentId_Returns404()
    {
        var (token, _) = await RegisterAndGetTokenAsync("photos-notfound@test.com");
        var authed = CreateAuthenticatedClient(token);

        var response = await authed.DeleteAsync($"/api/photos/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePhoto_SecondPhoto_Returns204()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("photos-delete-ok@test.com");

        // Seed two photos
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var keepId = Guid.NewGuid();
        var deleteId = Guid.NewGuid();
        db.Photos.AddRange(
            new Photo { Id = keepId,   UserId = userId, FileName = "keep.jpg",   DisplayOrder = 0, CreatedAt = DateTime.UtcNow },
            new Photo { Id = deleteId, UserId = userId, FileName = "delete.jpg", DisplayOrder = 1, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var authed = CreateAuthenticatedClient(token);
        var response = await authed.DeleteAsync($"/api/photos/{deleteId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a minimal multipart/form-data body with a 1×1 white PNG.</summary>
    private static MultipartFormDataContent BuildImageContent(string fileName)
    {
        // Minimal valid 1×1 white PNG bytes
        var pngBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk length + type
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // width=1, height=1
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, // bit depth, color type, etc.
            0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, // IDAT chunk
            0x54, 0x08, 0xD7, 0x63, 0xF8, 0xFF, 0xFF, 0x3F,
            0x00, 0x05, 0xFE, 0x02, 0xFE, 0xA7, 0x35, 0x81,
            0x84, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, // IEND chunk
            0x44, 0xAE, 0x42, 0x60, 0x82
        };

        // Use the raw bytes regardless of fileName extension — PhotoService only checks extension
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pngBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", fileName);
        return content;
    }
}
