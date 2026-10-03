using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

public class DeviceTokenService(PmfDbContext context, ILogger<DeviceTokenService> logger) : IDeviceTokenService
{
    public const int MaxDevicesPerUser = 10;

    public async Task<ServiceResult<DeviceTokenResponse>> RegisterAsync(int userId, DeviceTokenRequest request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Platform))
            return ServiceResult<DeviceTokenResponse>.Invalid("Platform must be Android, Ios or Web.");

        var token = request.PushToken.Trim();

        if (token.Length is < 16 or > 1024 || token.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            return ServiceResult<DeviceTokenResponse>.Invalid("PushToken is not a valid device token.");

        var now = DateTime.UtcNow;
        var platform = request.Platform.ToString();

        await using var tx = await context.Database.BeginTransactionAsync(ct);

        try
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "DeviceTokens" ("UserId", "Platform", "PushToken", "CreatedAt", "LastSeenAt")
                VALUES ({userId}, {platform}, {token}, {now}, {now})
                ON CONFLICT ("PushToken") DO UPDATE SET
                    "UserId"     = EXCLUDED."UserId",
                    "Platform"   = EXCLUDED."Platform",
                    "LastSeenAt" = EXCLUDED."LastSeenAt",
                    "CreatedAt"  = CASE WHEN "DeviceTokens"."UserId" <> EXCLUDED."UserId"
                                        THEN EXCLUDED."CreatedAt"
                                        ELSE "DeviceTokens"."CreatedAt" END
                """, ct);


            var evictIds = await context.DeviceTokens.AsNoTracking()
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.LastSeenAt).ThenByDescending(d => d.Id)
                .Skip(MaxDevicesPerUser)
                .Select(d => d.Id)
                .ToListAsync(ct);

            if (evictIds.Count > 0)
                await context.DeviceTokens.Where(d => evictIds.Contains(d.Id)).ExecuteDeleteAsync(ct);

            var saved = await context.DeviceTokens.AsNoTracking()
                .Where(d => d.PushToken == token && d.UserId == userId)
                .Select(d => new DeviceTokenResponse(d.Id, d.Platform, d.CreatedAt, d.LastSeenAt))
                .FirstOrDefaultAsync(ct);

            if (saved is null)
                return ServiceResult<DeviceTokenResponse>.Conflict("This device was registered to another account a moment ago. Register again.");

            await tx.CommitAsync(ct);

            // The token itself is never logged.
            logger.LogInformation("User {UserId} registered {Platform} device {DeviceTokenId}", userId, request.Platform, saved.Id);

            return ServiceResult<DeviceTokenResponse>.Ok(saved);
        }
        catch (Exception ex) when (PostgresErrors.IsForeignKeyViolation(ex))
        {
            return ServiceResult<DeviceTokenResponse>.Conflict("The account no longer exists.");
        }
    }

    public Task<List<DeviceTokenResponse>> GetMineAsync(int userId, CancellationToken ct) =>
        context.DeviceTokens.AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastSeenAt).ThenByDescending(d => d.Id)
            .Select(d => new DeviceTokenResponse(d.Id, d.Platform, d.CreatedAt, d.LastSeenAt))
            .ToListAsync(ct);

    public async Task<bool> RemoveAsync(int userId, int id, CancellationToken ct)
    {
          var rows = await context.DeviceTokens
            .Where(d => d.Id == id && d.UserId == userId)
            .ExecuteDeleteAsync(ct);

        if (rows > 0)
            logger.LogInformation("User {UserId} removed device {DeviceTokenId}", userId, id);

        return rows > 0;
    }

    public async Task<int> RemoveByTokenAsync(int userId, string pushToken, CancellationToken ct)
    {
        var token = pushToken.Trim();

        var rows = await context.DeviceTokens
            .Where(d => d.PushToken == token && d.UserId == userId)
            .ExecuteDeleteAsync(ct);

        if (rows > 0)
            logger.LogInformation("User {UserId} unregistered a device by token", userId);

        return rows;
    }

    public async Task<List<DeviceTarget>> GetTargetsForUsersAsync(IReadOnlyCollection<int> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return [];

        var ids = userIds.ToList();

        return await context.DeviceTokens.AsNoTracking()
            .Where(d => ids.Contains(d.UserId) && d.User.IsActive)
            .Select(d => new DeviceTarget(d.UserId, d.Platform, d.PushToken))
            .ToListAsync(ct);
    }
    public async Task<int> PruneAsync(IReadOnlyCollection<string> pushTokens, CancellationToken ct)
    {
        if (pushTokens.Count == 0) return 0;

        var tokens = pushTokens.ToList();

        var rows = await context.DeviceTokens
            .Where(d => tokens.Contains(d.PushToken))
            .ExecuteDeleteAsync(ct);

        if (rows > 0)
            logger.LogInformation("Pruned {Count} device tokens the push provider reported as invalid", rows);

        return rows;
    }
}