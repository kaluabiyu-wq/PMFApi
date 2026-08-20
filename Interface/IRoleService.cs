using PmfApi.Dto;
using PmfApi.Entities;

public interface IRoleService
{
Task<RoleResponse> CreateAsync(RoleRequest role,CancellationToken ct);
Task<RoleResponse?> GetByIdAsync(int id, CancellationToken ct);

}