using Microsoft.EntityFrameworkCore;
using RatingApp.Application.DTOs.Photos;
using RatingApp.Application.DTOs.Users;
using RatingApp.Domain.Entities;
using RatingApp.Domain.Enums;
using RatingApp.Infrastructure.Persistence;

namespace RatingApp.Application.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly PhotoService _photoService;

    public UserService(AppDbContext db, PhotoService photoService)
        => (_db, _photoService) = (db, photoService);

    public async Task<UserProfileDto> GetProfileAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        var photos = await _photoService.GetUserPhotosAsync(userId);
        return MapToDto(user, photos);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest req)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.DisplayName = req.DisplayName;
        user.Latitude = req.Latitude;
        user.Longitude = req.Longitude;

        await _db.SaveChangesAsync();
        var photos = await _photoService.GetUserPhotosAsync(userId);
        return MapToDto(user, photos);
    }

    public async Task<PreferenceDto> GetPreferencesAsync(Guid userId)
    {
        var pref = await _db.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId)
            ?? throw new KeyNotFoundException("Preferences not found.");
        return new PreferenceDto((int)pref.PreferredGender, pref.MinAge, pref.MaxAge, pref.MaxDistanceMiles);
    }

    public async Task<PreferenceDto> UpsertPreferencesAsync(Guid userId, PreferenceDto req)
    {
        var pref = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);

        if (pref is null)
        {
            pref = new UserPreference { Id = Guid.NewGuid(), UserId = userId };
            _db.UserPreferences.Add(pref);
        }

        pref.PreferredGender = (Gender)req.PreferredGender;
        pref.MinAge = req.MinAge;
        pref.MaxAge = req.MaxAge;
        pref.MaxDistanceMiles = req.MaxDistanceMiles;

        await _db.SaveChangesAsync();
        return req;
    }

    private static UserProfileDto MapToDto(AppUser user, List<PhotoDto> photos)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        int age = today.Year - user.Birthdate.Year;
        if (user.Birthdate > today.AddYears(-age)) age--;
        return new UserProfileDto(user.Id, user.Email, user.DisplayName,
            (int)user.Gender, user.Birthdate, user.Latitude, user.Longitude, age, photos);
    }
}
