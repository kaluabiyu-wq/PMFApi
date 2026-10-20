using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Interfaces;


public interface IAdminService
{
    Task<PagedResponse<AdminUserResponse>> ListUsersAsync(AdminUserQuery query, CancellationToken ct);

    Task<AdminUserDetailResponse?> GetUserAsync(int id, CancellationToken ct);

    Task<AdminUserCreatedResponse> CreateUserAsync(int actorId, AdminUserCreateRequest request, CancellationToken ct);

    Task<AdminUserDetailResponse?> UpdateUserAsync(int actorId, int id, AdminUserUpdateRequest request, CancellationToken ct);

    Task<AdminUserDetailResponse?> ChangeRoleAsync(int actorId, int id, int roleId, CancellationToken ct);

    Task<AdminUserDetailResponse?> SetStatusAsync(int actorId, int id, AccountStatus status, string? reason, CancellationToken ct);

    Task<bool> DeleteUserAsync(int actorId, int id, string confirmEmail, CancellationToken ct);

    Task<string?> ResetPasswordAsync(int actorId, int id, CancellationToken ct);

    Task<bool?> SendResetEmailAsync(int actorId, int id, CancellationToken ct);

    Task<IReadOnlyList<RoleSummaryResponse>> GetRolesAsync(CancellationToken ct);

    Task<AdminOverviewResponse> GetOverviewAsync(CancellationToken ct);

    Task<SystemHealthResponse> GetHealthAsync(CancellationToken ct);

    Task<PagedResponse<AuditLogResponse>> GetAuditAsync(AuditQuery query, CancellationToken ct);
}