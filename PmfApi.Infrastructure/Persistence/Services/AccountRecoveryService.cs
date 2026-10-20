using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

public class AccountRecoveryService(
    PmfDbContext context,
    IPasswordHasher<User> hasher,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<AccountRecoveryService> logger) : IAccountRecoveryService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task<bool> SendResetLinkAsync(User user, CancellationToken ct)
    {
    
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        user.PasswordResetTokenHash = Hash(token);
        user.PasswordResetExpiresAt = DateTime.UtcNow.Add(TokenLifetime);
        await context.SaveChangesAsync(ct);

        var baseUrl = (configuration["App:ClientBaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
        var link = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={token}";

        var body =
            $"Hello {user.FullName},\n\n" +
            "A password reset was requested for your PMAFS account.\n" +
            $"Open this link within 1 hour to choose a new password:\n\n{link}\n\n" +
            "If you did not expect this, you can ignore this email; your password has not changed.";

        var sent = await emailSender.SendAsync(user.Email, "Reset your PMAFS password", body, ct);

        // With no mail server configured, development can still finish the flow from the console.
        if (!sent && string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("DEV ONLY - reset link for {Email}: {Link}", user.Email, link);
        }

        return sent;
    }

    public async Task RequestResetAsync(string email, CancellationToken ct)
    {
        var user = await context.Users.FirstOrDefaultAsync(u =>
            u.Email == email.Trim() && u.IsActive && u.SuspendedAt == null && u.DeletedAt == null, ct);

        if (user is not null)
            await SendResetLinkAsync(user, ct);
    }

    public async Task<bool> ResetAsync(string email, string token, string newPassword, CancellationToken ct)
    {
        var user = await context.Users.FirstOrDefaultAsync(u =>
            u.Email == email.Trim() && u.IsActive && u.SuspendedAt == null && u.DeletedAt == null, ct);

        if (user?.PasswordResetTokenHash is null
            || user.PasswordResetExpiresAt is null
            || user.PasswordResetExpiresAt < DateTime.UtcNow)
            return false;

        var expected = Encoding.UTF8.GetBytes(user.PasswordResetTokenHash);
        var actual = Encoding.UTF8.GetBytes(Hash(token));
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            return false;

        user.Password = hasher.HashPassword(user, newPassword);

     
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiresAt = null;
        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} reset their password with an emailed link", user.Id);
        return true;
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}