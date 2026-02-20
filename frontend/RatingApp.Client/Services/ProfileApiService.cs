using System.Net.Http.Json;
using RatingApp.Client.Models;

namespace RatingApp.Client.Services;

public class ProfileApiService
{
    private readonly HttpClient _http;

    public ProfileApiService(HttpClient http) => _http = http;

    public Task<UserProfileDto?> GetProfileAsync() =>
        _http.GetFromJsonAsync<UserProfileDto>("api/me");

    public async Task<UserProfileDto?> UpdateProfileAsync(UpdateProfileRequest req)
    {
        var response = await _http.PutAsJsonAsync("api/me", req);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UserProfileDto>()
            : null;
    }

    public Task<PreferenceDto?> GetPreferencesAsync() =>
        _http.GetFromJsonAsync<PreferenceDto>("api/me/preferences");

    public async Task<PreferenceDto?> UpdatePreferencesAsync(PreferenceDto req)
    {
        var response = await _http.PutAsJsonAsync("api/me/preferences", req);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PreferenceDto>()
            : null;
    }

    public Task<RatingSummaryDto?> GetRatingSummaryAsync() =>
        _http.GetFromJsonAsync<RatingSummaryDto>("api/me/rating-summary");
}
