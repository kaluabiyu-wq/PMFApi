using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IDeviceTokenService
{
    Task<ServiceResult<DeviceTokenResponse>> RegisterAsync(int userId, DeviceTokenRequest request, CancellationToken ct);
    Task<List<DeviceTokenResponse>> GetMineAsync(int userId, CancellationToken ct);
    Task<bool> RemoveAsync(int userId, int id, CancellationToken ct);
    Task<int> RemoveByTokenAsync(int userId, string pushToken, CancellationToken ct);
    Task<List<DeviceTarget>> GetTargetsForUsersAsync(IReadOnlyCollection<int> userIds, CancellationToken ct);
    Task<int> PruneAsync(IReadOnlyCollection<string> pushTokens, CancellationToken ct);
}