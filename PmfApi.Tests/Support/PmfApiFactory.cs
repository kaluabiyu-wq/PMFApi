using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Tests.Support;


public sealed class PmfApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    
    public const string UnusedConnectionString =
        "Host=localhost;Port=1;Database=pmf_unused;Username=unused;Password=unused;Timeout=1";


    public const string JwtKey = "pmf-test-signing-key-0123456789-abcdefghijklmnop";
    public const string JwtIssuer = "PmfApi";
    public const string JwtAudience = "PmfClient";

    private readonly Dictionary<string, string?> _settings = new()
    {
        ["ConnectionStrings:PmfDatabase"] = connectionString,
        ["Jwt:Key"] = JwtKey,
        ["Jwt:Issuer"] = JwtIssuer,
        ["Jwt:Audience"] = JwtAudience,
        ["Jwt:ExpiryMinutes"] = "60",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        
        foreach (var (key, value) in _settings)
            builder.UseSetting(key, value);

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(_settings));
    }

    
    public HttpClient ClientFor(TestUser? user)
    {
        var client = CreateClient();
        if (user is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(user));
        return client;
    }

    public string TokenFor(TestUser user)
    {
        using var scope = Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var entity = new User
        {
            Id = user.Id,
            FullName = user.Email,
            Email = user.Email,
            Password = "unused",
            RoleId = 0,
            Role = new Role { Name = user.Role },
        };

        return tokens.GenerateToken(entity).Token;
    }
}
