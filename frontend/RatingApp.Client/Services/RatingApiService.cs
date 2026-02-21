using System.Net.Http.Json;
using RatingApp.Client.Models;

namespace RatingApp.Client.Services;

public class RatingApiService
{
    private readonly HttpClient _http;

    public RatingApiService(HttpClient http) => _http = http;

    public async Task<List<CriterionModel>> GetCriteriaAsync()
    {
        return await _http.GetFromJsonAsync<List<CriterionModel>>("api/ratings/criteria")
            ?? new List<CriterionModel>();
    }

    public async Task<RatingResult?> SubmitRatingAsync(Guid ratedUserId, List<CriterionScoreModel> criteriaScores, string? comment = null)
    {
        var response = await _http.PostAsJsonAsync("api/ratings",
            new MultiCriteriaRatingRequest(ratedUserId, criteriaScores, comment));
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<RatingResult>()
            : null;
    }

    public async Task<AggregateRatingModel?> GetAggregateAsync(Guid candidateId)
    {
        return await _http.GetFromJsonAsync<AggregateRatingModel>(
            $"api/ratings/candidates/{candidateId}/aggregate");
    }
}