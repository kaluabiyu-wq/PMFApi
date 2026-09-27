using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;


public class TokenService(IConfiguration configuration) : ITokenService
{
    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var jwtSection = configuration.GetSection("Jwt");

        var key = jwtSection["Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key is not configured. Set it in appsettings, an environment " +
                "variable, or a secret store before calling GenerateToken.");

        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
         var expiryMinutesRaw = jwtSection["ExpiryMinutes"];
        var expiryMinutes = int.TryParse(expiryMinutesRaw, out var parsedExpiryMinutes)
            ? parsedExpiryMinutes
            : 60;
        
         var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Name),
        };


        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        return (handler.WriteToken(token), expiresAt);
    }
}