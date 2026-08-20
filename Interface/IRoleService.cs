using PmfApi.Dto;
using PmfApi.Entities;


namespace PmfApi.Interface;

public interface IRoleService
{
Task<RoleResponse> CreateAsync(RoleRequest role,CancellationToken ct);
Task<RoleResponse?> GetByIdAsync(int id, CancellationToken ct);

}