using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PmfApi.Application.Abstractions;
using PmfApi.Application.Dtos;
using PmfApi.Application.Exceptions;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

public class AccountService(
    PmfDbContext context,
    IPasswordHasher<User> hasher,
    IMemoryCache cache,
    ILogger<AccountService> logger) : IAccountService
{
    public Task<AccountResponse?> GetAsync(int userId, CancellationToken ct) =>
        context.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new AccountResponse(
                u.Id, u.FullName, u.Email, u.Role.Name, u.CreatedAt,
                u.LocationId, u.Location.Label, u.Location.Subcity, u.Location.Woreda,
                u.Location.Coordinate.Latitude, u.Location.Coordinate.Longitude,
                context.Orders.Count(o => o.UserId == u.Id),
                context.Orders.Count(o => o.UserId == u.Id
                    && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed)),
                context.Favorites.Count(f => f.UserId == u.Id),
                context.Reviews.Count(r => r.UserId == u.Id)))
            .FirstOrDefaultAsync(ct);

    public async Task<AccountResponse?> UpdateAsync(int userId, AccountUpdateRequest request, CancellationToken ct)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
        if (user is null) return null;

        var email = request.Email.Trim();
        if (email != user.Email
            && await context.Users.AnyAsync(u => u.Id != userId && u.Email == email, ct))
            throw new DuplicateEmailException();

        if (request.LocationId is { } locationId && locationId != user.LocationId)
        {
            if (!await context.Locations.AnyAsync(l => l.Id == locationId, ct))
                throw new LocationNotFoundException();
            user.LocationId = locationId;
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} updated their account details", userId);
        return await GetAsync(userId, ct);
    }

    public async Task<bool?> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
        if (user is null) return null;

        if (hasher.VerifyHashedPassword(user, user.Password, request.CurrentPassword)
            == PasswordVerificationResult.Failed)
            return false;

        user.Password = hasher.HashPassword(user, request.NewPassword);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} changed their password", userId);
        return true;
    }

    public async Task<AccountDeleteOutcome> DeleteAsync(int userId, string password, CancellationToken ct)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
        if (user is null) return AccountDeleteOutcome.NotFound;

        if (hasher.VerifyHashedPassword(user, user.Password, password) == PasswordVerificationResult.Failed)
            return AccountDeleteOutcome.WrongPassword;

        // A pharmacy may be preparing an order for this person; don't leave it dangling.
        if (await context.Orders.AnyAsync(o => o.UserId == userId
                && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed), ct))
            return AccountDeleteOutcome.HasOpenOrders;

        // Soft delete keeps orders, reviews and prescriptions that other people's records point at.
        user.IsActive = false;

        // Stop pushing notifications to a closed account's devices.
        var tokens = await context.DeviceTokens.Where(d => d.UserId == userId).ToListAsync(ct);
        context.DeviceTokens.RemoveRange(tokens);

        await context.SaveChangesAsync(ct);
        cache.Remove(UserCacheKeys.CanUseApi(userId));

        logger.LogInformation("User {UserId} deactivated their account", userId);
        return AccountDeleteOutcome.Deleted;
    }
}