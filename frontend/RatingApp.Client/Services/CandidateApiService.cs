using System.Net.Http.Json;
using RatingApp.Client.Models;

namespace RatingApp.Client.Services;

public class CandidateApiService
{
    private readonly HttpClient _http;

    public CandidateApiService(HttpClient http) => _http = http;

    public Task<List<CandidateDto>?> GetCandidatesAsync(int pageSize = 10) =>
        _http.GetFromJsonAsync<List<CandidateDto>>($"api/candidates?pageSize={pageSize}");
}
