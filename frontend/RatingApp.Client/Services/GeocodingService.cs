using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RatingApp.Client.Services;

public class GeocodingResult
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string DisplayName { get; set; } = "";
}

public class GeocodingService
{
    private readonly HttpClient _http;

    public GeocodingService(HttpClient http) => _http = http;

    public async Task<List<GeocodingResult>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return [];

        try
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count=8&language=en&format=json";
            var resp = await _http.GetFromJsonAsync<OpenMeteoResponse>(url);
            return resp?.Results?
                .Select(r => new GeocodingResult
                {
                    Latitude = r.Latitude,
                    Longitude = r.Longitude,
                    DisplayName = BuildName(r.Name, r.Admin1, r.Country)
                })
                .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<string> ReverseGeocodeAsync(double lat, double lon)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/reverse?lat={lat}&lon={lon}&format=json";
            var resp = await _http.GetFromJsonAsync<NominatimResponse>(url);
            if (resp?.Address is { } addr)
            {
                var city = addr.City ?? addr.Town ?? addr.Village;
                return BuildName(city, addr.State, addr.Country);
            }
        }
        catch { }

        return "Location set";
    }

    private static string BuildName(string? a, string? b, string? c) =>
        string.Join(", ", new[] { a, b, c }.Where(s => !string.IsNullOrEmpty(s)));

    // ── Open-Meteo models ────────────────────────────────────────────

    private sealed class OpenMeteoResponse
    {
        public List<OpenMeteoResult>? Results { get; set; }
    }

    private sealed class OpenMeteoResult
    {
        public string? Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Country { get; set; }
        [JsonPropertyName("admin1")]
        public string? Admin1 { get; set; }
    }

    // ── Nominatim models ─────────────────────────────────────────────

    private sealed class NominatimResponse
    {
        public NominatimAddress? Address { get; set; }
    }

    private sealed class NominatimAddress
    {
        public string? City { get; set; }
        public string? Town { get; set; }
        public string? Village { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
    }
}
