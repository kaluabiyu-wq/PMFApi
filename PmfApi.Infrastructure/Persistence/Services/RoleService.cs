using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PmfApi.Infrastructure.Persistence.Services;


public class RoleService(PmfDbContext context, ILogger<RoleService> logger) : IRoleService
{
    public Task<RoleResponse?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Roles.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RoleResponse(r.Id, r.Name, r.Description))
            .FirstOrDefaultAsync(ct);

    public async Task<RoleResponse> CreateAsync(RoleRequest request, CancellationToken ct)
    {
        var nameExists = await context.Roles .AsNoTracking()
            .AnyAsync(r => r.Name == request.Name, ct);

        if (nameExists)
        {
            logger.LogWarning("Attempted to create duplicate role with name {RoleName}", request.Name);
            return null; 
        }

        var role = new Role
        {
        
            Name = request.Name,
            Description = request.Description
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync(ct);

        return new RoleResponse(role.Id, role.Name, role.Description);
    }
}