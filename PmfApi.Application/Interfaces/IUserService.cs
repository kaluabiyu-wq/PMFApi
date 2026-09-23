
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;


namespace PmfApi.Application.Interfaces;

public interface IUserService
{
Task<UserResponse> CreateAsync(UserRequest user,CancellationToken ct);
Task<UserResponse?> GetByeEmailAsync(string email, CancellationToken ct);

Task<UserResponse?> GetByIdAsync(int id, CancellationToken ct);

Task<PagedResponse<UserResponse>> GetUserAsync(PagedRequest request, CancellationToken ct);

}