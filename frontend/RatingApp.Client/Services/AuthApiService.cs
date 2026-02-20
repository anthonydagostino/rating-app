using System.Net.Http.Json;
using RatingApp.Client.Auth;
using RatingApp.Client.Models;

namespace RatingApp.Client.Services;

public class AuthApiService
{
    private readonly HttpClient _http;
    private readonly CustomAuthStateProvider _authState;

    public AuthApiService(HttpClient http, CustomAuthStateProvider authState)
        => (_http, _authState) = (http, authState);

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest req)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", req);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, error);
            }
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result is not null)
                await _authState.NotifyLoginAsync(result.Token);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login",
                new LoginRequest(email, password));
            if (!response.IsSuccessStatusCode)
                return (false, "Invalid email or password.");
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result is not null)
                await _authState.NotifyLoginAsync(result.Token);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task LogoutAsync() => await _authState.NotifyLogoutAsync();
}
