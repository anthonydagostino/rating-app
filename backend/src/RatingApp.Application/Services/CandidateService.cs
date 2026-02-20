using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RatingApp.Application.DTOs.Candidates;
using RatingApp.Infrastructure.Persistence;

namespace RatingApp.Application.Services;

public class CandidateService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CandidateService> _logger;
    private readonly PhotoService _photoService;
    private const double EarthRadiusMiles = 3958.8;

    public CandidateService(AppDbContext db, ILogger<CandidateService> logger, PhotoService photoService)
        => (_db, _logger, _photoService) = (db, logger, photoService);

    public async Task<List<CandidateDto>> GetCandidatesAsync(Guid currentUserId, int pageSize = 10)
    {
        var me = await _db.Users
            .Include(u => u.Preference)
            .FirstOrDefaultAsync(u => u.Id == currentUserId)
            ?? throw new KeyNotFoundException("User not found.");

        var pref = me.Preference
            ?? throw new InvalidOperationException("Preferences not configured.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var minDob = today.AddYears(-pref.MaxAge);
        var maxDob = today.AddYears(-pref.MinAge);

        var ratedIds = await _db.Ratings
            .Where(r => r.RaterUserId == currentUserId)
            .Select(r => r.RatedUserId)
            .ToListAsync();

        // Users who have at least 1 photo
        var usersWithPhotos = await _db.Photos
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync();

        double latDelta = pref.MaxDistanceMiles / 69.0;
        double lonDelta = pref.MaxDistanceMiles /
            (69.0 * Math.Cos(me.Latitude * Math.PI / 180.0));

        var candidates = await _db.Users
            .Where(u =>
                u.Id != currentUserId &&
                u.Gender == pref.PreferredGender &&
                u.Birthdate >= minDob &&
                u.Birthdate <= maxDob &&
                !ratedIds.Contains(u.Id) &&
                usersWithPhotos.Contains(u.Id) &&
                u.Latitude >= me.Latitude - latDelta &&
                u.Latitude <= me.Latitude + latDelta &&
                u.Longitude >= me.Longitude - lonDelta &&
                u.Longitude <= me.Longitude + lonDelta)
            .Take(pageSize * 5)
            .ToListAsync();

        var filtered = candidates
            .Select(u => new
            {
                User = u,
                Distance = HaversineDistance(me.Latitude, me.Longitude, u.Latitude, u.Longitude)
            })
            .Where(x => x.Distance <= pref.MaxDistanceMiles)
            .OrderBy(x => x.Distance)
            .Take(pageSize)
            .ToList();

        // Load photos for all filtered candidates in one query
        var candidateIds = filtered.Select(x => x.User.Id).ToList();
        var allPhotos = await _db.Photos
            .Where(p => candidateIds.Contains(p.UserId))
            .OrderBy(p => p.UserId)
            .ThenBy(p => p.DisplayOrder)
            .ToListAsync();

        var photosByUser = allPhotos.GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return filtered.Select(x =>
        {
            var photos = photosByUser.TryGetValue(x.User.Id, out var list)
                ? list.Select(p => _photoService.BuildUrl(x.User.Id, p.FileName)).ToList()
                : new List<string>();

            return new CandidateDto(
                x.User.Id,
                x.User.DisplayName,
                CalculateAge(x.User.Birthdate),
                (int)x.User.Gender,
                Math.Round(x.Distance, 1),
                photos);
        }).ToList();
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lon2 - lon1);
        double a = Math.Pow(Math.Sin(dLat / 2), 2)
                 + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                 * Math.Pow(Math.Sin(dLon / 2), 2);
        double c = 2 * Math.Asin(Math.Sqrt(a));
        return EarthRadiusMiles * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;

    private static int CalculateAge(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        int age = today.Year - dob.Year;
        if (dob > today.AddYears(-age)) age--;
        return age;
    }
}
