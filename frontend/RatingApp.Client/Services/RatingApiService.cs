using System.Net.Http.Json;
using RatingApp.Client.Models;

namespace RatingApp.Client.Services;

public class RatingApiService
{
    private readonly HttpClient _http;

    public RatingApiService(HttpClient http) => _http = http;

    public async Task<RatingResult?> SubmitRatingAsync(Guid ratedUserId, int score, string? comment = null)
    {
        var response = await _http.PostAsJsonAsync("api/ratings",
            new SimpleRatingRequest(ratedUserId, score, comment));
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<RatingResult>()
            : null;
    }

    public async Task<RatingSummaryModel?> GetRatingSummaryAsync(Guid userId)
    {
        return await _http.GetFromJsonAsync<RatingSummaryModel>($"api/ratings/summary/{userId}");
    }
}
