using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IFavoriteService
{
    Task<FavoriteResponse> CreateAsync(int userId, FavoriteRequest request, CancellationToken ct);

    Task<FavoriteResponse?> GetByUserIdAsync(int userId, int id, CancellationToken ct);

    Task<List<FavoriteResponse>> GetByUserAsync(int userId, CancellationToken ct);

    Task<bool> IsFavoritedAsync(int userId, int pharmacyId, CancellationToken ct);

    Task<bool> DeleteAsync(int userId, int id, CancellationToken ct);
}
