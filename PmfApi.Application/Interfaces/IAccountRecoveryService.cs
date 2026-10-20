using PmfApi.Domain.Entities;

namespace PmfApi.Application.Interfaces;

public interface IAccountRecoveryService
{
   
    Task<bool> SendResetLinkAsync(User user, CancellationToken ct);

    Task RequestResetAsync(string email, CancellationToken ct);

    Task<bool> ResetAsync(string email, string token, string newPassword, CancellationToken ct);
}