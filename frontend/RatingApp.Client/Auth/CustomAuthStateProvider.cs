using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RatingApp.Client.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public CustomAuthStateProvider(ILocalStorageService localStorage)
        => _localStorage = localStorage;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync("jwt");

            if (string.IsNullOrWhiteSpace(token))
                return Anonymous;

            var claims = ParseClaimsFromJwt(token);

            // Check expiry
            var expClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp);
            if (expClaim is not null)
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim.Value));
                if (expDate < DateTimeOffset.UtcNow)
                {
                    await _localStorage.RemoveItemAsync("jwt");
                    return Anonymous;
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return Anonymous;
        }
    }

    public async Task NotifyLoginAsync(string token)
    {
        await _localStorage.SetItemAsStringAsync("jwt", token);
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task NotifyLogoutAsync()
    {
        await _localStorage.RemoveItemAsync("jwt");
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    public async Task<Guid?> GetCurrentUserIdAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync("jwt");
            if (string.IsNullOrWhiteSpace(token)) return null;
            var claims = ParseClaimsFromJwt(token);
            var sub = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            return sub is not null ? Guid.Parse(sub) : null;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        return token.Claims;
    }
}
