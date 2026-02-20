using System.Net.Http.Json;
using RatingApp.Client.Models;

namespace RatingApp.Client.Services;

public class RatingApiService
{
    private readonly HttpClient _http;

    public RatingApiService(HttpClient http) => _http = http;

    public async Task<RatingResult?> SubmitRatingAsync(Guid ratedUserId, int score)
    {
        var response = await _http.PostAsJsonAsync("api/ratings",
            new SubmitRatingRequest(ratedUserId, score));
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<RatingResult>()
            : null;
    }
}
