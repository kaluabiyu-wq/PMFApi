using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public enum AccountDeleteOutcome { Deleted, WrongPassword, HasOpenOrders, NotFound }

public interface IAccountService
{
    Task<AccountResponse?> GetAsync(int userId, CancellationToken ct);

    
    Task<AccountResponse?> UpdateAsync(int userId, AccountUpdateRequest request, CancellationToken ct);
   
    Task<bool?> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct);

    Task<AccountDeleteOutcome> DeleteAsync(int userId, string password, CancellationToken ct);
}