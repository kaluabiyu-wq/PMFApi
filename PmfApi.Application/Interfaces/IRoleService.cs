
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;


namespace PmfApi.Application.Interfaces;

public interface IRoleService
{
Task<RoleResponse> CreateAsync(RoleRequest role,CancellationToken ct);
Task<RoleResponse?> GetByIdAsync(int id, CancellationToken ct);


}