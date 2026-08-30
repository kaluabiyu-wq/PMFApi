using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PmfApi.Infrastructure.Persistence.Services;

public class FavoriteService(PmfDbContext context, ILogger<FavoriteService> logger)
: IFavoriteService
{
    public async Task<FavoriteResponse> CreateAsync(int userId, FavoriteRequest request, CancellationToken ct)
    {
        var favorite = new Favorite
        {
            UserId = userId,
            PharmacyId = request.PharmacyId,
            CreatedAt = DateTime.UtcNow,
        };

        context.Favorites.Add(favorite);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} favorited Pharmacy {PharmacyId} (FavoriteId {FavoriteId})",
            favorite.UserId, favorite.PharmacyId, favorite.Id);

        return (await GetByUserIdAsync(favorite.UserId, favorite.Id, ct))!;
    }

    public Task<FavoriteResponse?> GetByUserIdAsync(int userId, int id, CancellationToken ct) =>
    context.Favorites.AsNoTracking()
    .Where(f => f.Id == id && f.UserId == userId)
    .Select(f => new FavoriteResponse(f.Id, f.UserId, f.PharmacyId, f.CreatedAt))
    .FirstOrDefaultAsync(ct);

    public Task<List<FavoriteResponse>> GetByUserAsync(int userId, CancellationToken ct) =>
    context.Favorites.AsNoTracking()
    .Where(f => f.UserId == userId)
    .OrderByDescending(f => f.CreatedAt)
    .Select(f => new FavoriteResponse(f.Id, f.UserId, f.PharmacyId, f.CreatedAt))
    .ToListAsync(ct);

    public Task<bool> IsFavoritedAsync(int userId, int pharmacyId, CancellationToken ct) =>
    context.Favorites.AsNoTracking()
    .AnyAsync(f => f.UserId == userId && f.PharmacyId == pharmacyId, ct);

    public async Task<bool> DeleteAsync(int userId, int id, CancellationToken ct)
    {
        var favorite = await context.Favorites
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, ct);

        if (favorite is null) return false;

        context.Favorites.Remove(favorite);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} unfavorited Pharmacy {PharmacyId} (FavoriteId {FavoriteId})",
            favorite.UserId, favorite.PharmacyId, favorite.Id);

        return true;
    }
}
