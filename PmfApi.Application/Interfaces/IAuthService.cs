using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IAuthService
{
   
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct);
}