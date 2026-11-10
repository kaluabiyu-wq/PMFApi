using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PmfApi.Api.Authorization;
using PmfApi.Tests.Support;

namespace PmfApi.Tests;

public sealed class WiringFixture : IDisposable
{
    // No database is needed here: the host builds without opening a connection, and every request below
    // is rejected before it would reach one.
    public PmfApiFactory Factory { get; } = new(PmfApiFactory.UnusedConnectionString);

    public void Dispose() => Factory.Dispose();
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// Step 6/7: the handlers are only half of authorization. The other half is Program.cs: which named
// policy carries which requirement, and whether each handler is actually registered. Both failures are
// silent. A policy name pointing at the wrong requirement, or a handler that was written but never added
// to DI, does not throw; the requirement simply can never be satisfied. These tests boot the real
// Program.cs and read the wiring back out.
// ─────────────────────────────────────────────────────────────────────────────────────────────────
public class AuthorizationWiringTests(WiringFixture fx) : IClassFixture<WiringFixture>
{
    private async Task<AuthorizationPolicy> PolicyAsync(string name)
    {
        var provider = fx.Factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = await provider.GetPolicyAsync(name);
        Assert.True(policy is not null, $"Policy '{name}' is not defined in Program.cs. Anything that asks for it will fail.");
        return policy!;
    }

    [Fact]
    public async Task PharmacyOwnerOrAdmin_allows_platform_admins_and_requires_an_elevated_staff_role()
    {
        var policy = await PolicyAsync("PharmacyOwnerOrAdmin");

        var requirement = Assert.Single(policy.Requirements.OfType<PharmacyOwnerRequirement>());
        Assert.True(requirement.AllowPlatformAdmin);
        Assert.True(requirement.RequireElevatedStaffRole);
    }

    [Fact]
    public async Task PharmacyOwner_is_the_strict_variant_with_no_admin_bypass()
    {
        var policy = await PolicyAsync("PharmacyOwner");

        var requirement = Assert.Single(policy.Requirements.OfType<PharmacyOwnerRequirement>());
        Assert.False(requirement.AllowPlatformAdmin);
        Assert.False(requirement.RequireElevatedStaffRole);
    }

    [Fact]
    public async Task DocumentReview_carries_the_document_review_requirement()
    {
        var policy = await PolicyAsync("DocumentReview");

        Assert.Single(policy.Requirements.OfType<PharmacyDocumentReviewRequirement>());
    }

    [Fact]
    public async Task PrescriptionVerification_carries_the_prescription_verification_requirement()
    {
        var policy = await PolicyAsync("PrescriptionVerification");

        Assert.Single(policy.Requirements.OfType<PrescriptionVerificationRequirement>());
    }

    [Fact]
    public void Every_authorization_handler_in_the_API_assembly_is_registered_in_DI()
    {
        // Handlers are scoped (they take the DbContext), so they must be resolved from a scope.
        using var scope = fx.Factory.Services.CreateScope();
        var registered = scope.ServiceProvider.GetServices<IAuthorizationHandler>()
            .Select(h => h.GetType())
            .ToHashSet();

        var defined = typeof(PharmacyOwnerHandler).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IAuthorizationHandler).IsAssignableFrom(t));

        foreach (var handler in defined)
            Assert.True(registered.Contains(handler),
                $"{handler.Name} exists but is not registered with AddScoped<IAuthorizationHandler, ...>. " +
                "Its requirement can never succeed, and nothing will tell you.");
    }

    // ── authentication at the edge: no database involved ──

    [Theory]
    [InlineData("GET", "/api/orders")]
    [InlineData("GET", "/api/prescriptions/1")]
    [InlineData("PATCH", "/api/prescriptions/1/review")]
    [InlineData("POST", "/api/orders/1/prescriptions")]
    [InlineData("POST", "/api/orders/1/confirm")]
    [InlineData("GET", "/api/pharmacies/1/prescriptions")]
    [InlineData("GET", "/api/alerts")]
    public async Task Order_prescription_and_alert_endpoints_reject_anonymous_callers(string method, string url)
    {
        var response = await fx.Factory.ClientFor(null).SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_signed_with_the_wrong_key_is_rejected()
    {
        var forged = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: PmfApiFactory.JwtIssuer,
            audience: PmfApiFactory.JwtAudience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, RoleNames.SystemAdmin),
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("an-attacker-key-that-is-long-enough-0123456789")),
                SecurityAlgorithms.HmacSha256)));

        var client = fx.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        var response = await client.GetAsync(Routes.Alerts);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_expired_token_is_rejected()
    {
        var expired = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: PmfApiFactory.JwtIssuer,
            audience: PmfApiFactory.JwtAudience,
            claims: [new Claim(ClaimTypes.NameIdentifier, "1")],
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(PmfApiFactory.JwtKey)),
                SecurityAlgorithms.HmacSha256)));

        var client = fx.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired);

        var response = await client.GetAsync(Routes.Alerts);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
