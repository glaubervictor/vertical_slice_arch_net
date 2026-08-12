using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace ArchNet.Web.Auth;

public sealed class JwtAuthStateProvider(ITokenService tokenService) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenService.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserAuthenticated(string token)
    {
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void NotifyUserLoggedOut()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return [];

        var payload = parts[1];

        // Pad Base64Url to standard Base64
        var remainder = payload.Length % 4;
        switch (remainder)
        {
            case 2:
                payload += "==";
                break;
            case 3:
                payload += "=";
                break;
        }

        payload = payload.Replace('-', '+').Replace('_', '/');

        var jsonBytes = Convert.FromBase64String(payload);
        var jsonString = Encoding.UTF8.GetString(jsonBytes);

        using var doc = JsonDocument.Parse(jsonString);
        var claims = new List<Claim>();

        foreach (var element in doc.RootElement.EnumerateObject())
        {
            var claimType = element.Name switch
            {
                "sub" => ClaimTypes.NameIdentifier,
                "unique_name" => ClaimTypes.Name,
                "role" => ClaimTypes.Role,
                _ => element.Name
            };

            switch (element.Value.ValueKind)
            {
                case JsonValueKind.Array:
                {
                    foreach (var item in element.Value.EnumerateArray())
                        if (item.ValueKind == JsonValueKind.String)
                            claims.Add(new Claim(claimType, item.GetString() ?? string.Empty));
                    break;
                }
                case JsonValueKind.String:
                    claims.Add(new Claim(claimType, element.Value.GetString() ?? string.Empty));
                    break;
            }
        }

        return claims;
    }
}
