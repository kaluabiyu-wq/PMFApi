using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;


public interface IAlertService
{
    Task<PagedResponse<AlertResponse>> GetMineAsync(int userId, AlertListQuery query,
     CancellationToken ct);

    Task<int> CountUnreadAsync(int userId, CancellationToken ct);

    Task<bool> MarkReadAsync(int userId, int id, CancellationToken ct);

    Task<int> MarkAllReadAsync(int userId, CancellationToken ct);
}