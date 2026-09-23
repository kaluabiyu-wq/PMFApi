using PmfApi.Dto;
using PmfApi.Entities;

public interface IUserService
{
Task<UserResponse> CreateAsync(UserRequest user,CancellationToken ct);
Task<UserResponse?> GetByeEmailAsync(string email, CancellationToken ct);

    Task<PagedResponse<UserResponse>> GetUserAsync(PagedRequest request, CancellationToken ct);

}