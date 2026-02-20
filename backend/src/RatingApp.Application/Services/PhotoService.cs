using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RatingApp.Application.DTOs.Photos;
using RatingApp.Domain.Entities;
using RatingApp.Infrastructure.Persistence;

namespace RatingApp.Application.Services;

public class PhotoService
{
    private readonly AppDbContext _db;
    private readonly string _uploadsRoot;
    private readonly string _baseUrl;
    private const int MaxPhotos = 6;

    public PhotoService(AppDbContext db, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _uploadsRoot = Path.Combine(env.WebRootPath, "uploads");
        _baseUrl = config["ApiBaseUrl"] ?? "http://localhost:5212";
        Directory.CreateDirectory(_uploadsRoot);
    }

    public async Task<List<PhotoDto>> GetUserPhotosAsync(Guid userId)
    {
        var photos = await _db.Photos
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync();

        return photos.Select(p => ToDto(p, userId)).ToList();
    }

    public async Task<PhotoDto> UploadPhotoAsync(Guid userId, IFormFile file)
    {
        var count = await _db.Photos.CountAsync(p => p.UserId == userId);
        if (count >= MaxPhotos)
            throw new InvalidOperationException($"Maximum of {MaxPhotos} photos allowed.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            throw new InvalidOperationException("Only JPG, PNG, or WebP images are allowed.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("File must be under 5 MB.");

        var userDir = Path.Combine(_uploadsRoot, userId.ToString());
        Directory.CreateDirectory(userDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(userDir, fileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        var photo = new Photo
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileName = fileName,
            DisplayOrder = count,
            CreatedAt = DateTime.UtcNow
        };

        _db.Photos.Add(photo);
        await _db.SaveChangesAsync();

        return ToDto(photo, userId);
    }

    public async Task DeletePhotoAsync(Guid userId, Guid photoId)
    {
        var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId)
            ?? throw new KeyNotFoundException("Photo not found.");

        var count = await _db.Photos.CountAsync(p => p.UserId == userId);
        if (count <= 1)
            throw new InvalidOperationException("You must keep at least 1 photo.");

        var filePath = Path.Combine(_uploadsRoot, userId.ToString(), photo.FileName);
        if (File.Exists(filePath))
            File.Delete(filePath);

        _db.Photos.Remove(photo);
        await _db.SaveChangesAsync();
    }

    public string BuildUrl(Guid userId, string fileName) =>
        $"{_baseUrl}/uploads/{userId}/{fileName}";

    private PhotoDto ToDto(Photo photo, Guid userId) =>
        new(photo.Id, BuildUrl(userId, photo.FileName), photo.DisplayOrder);
}
