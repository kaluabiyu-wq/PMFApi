using System.Security.Claims;

namespace PmfApi.Tests.Support;

public static class Principals
{
    
    public static ClaimsPrincipal Create(int? userId, params string[] roles)
    {
        var claims = new List<Claim>();
        if (userId is { } id) claims.Add(new Claim(ClaimTypes.NameIdentifier, id.ToString()));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

    
    public static ClaimsPrincipal WithRawUserId(string rawId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, rawId) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

    
    public static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());
}
