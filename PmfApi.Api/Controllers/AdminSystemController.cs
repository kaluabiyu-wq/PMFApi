using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;

namespace PmfApi.Api.Controllers;

/// <summary>Dashboard data, roles, activity log and system health for system administrators.</summary>
[ApiController]
[Authorize(Roles = "SysAdmin")]
[Route("api/admin")]
[Tags("Admin - System")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AdminSystemController(IAdminService adminService) : ControllerBase
{
    [HttpGet("overview", Name = nameof(GetOverview))]
    [ProducesResponseType(typeof(AdminOverviewResponse), StatusCodes.Status200OK)]
    [EndpointSummary("Dashboard numbers")]
    [EndpointDescription("User totals and sign-ups, active / suspended / inactive counts, pharmacies, medicine catalogue and inventory listings, pending reviews, and the ten most recent administrative actions.")]
    public async Task<IActionResult> GetOverview(CancellationToken ct) =>
        Ok(await adminService.GetOverviewAsync(ct));

    [HttpGet("health", Name = nameof(GetHealth))]
    [ProducesResponseType(typeof(SystemHealthResponse), StatusCodes.Status200OK)]
    [EndpointSummary("API and system health")]
    [EndpointDescription("Database reachability and latency, uptime, memory, whether email is configured, and the list of important system alerts (pending verifications, stale reviews, single administrator, and so on).")]
    public async Task<IActionResult> GetHealth(CancellationToken ct) =>
        Ok(await adminService.GetHealthAsync(ct));

    [HttpGet("roles", Name = nameof(GetRoles))]
    [ProducesResponseType(typeof(IReadOnlyList<RoleSummaryResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("Roles with user counts and what each may do")]
    public async Task<IActionResult> GetRoles(CancellationToken ct) =>
        Ok(await adminService.GetRolesAsync(ct));

    [HttpGet("audit", Name = nameof(GetAudit))]
    [ProducesResponseType(typeof(PagedResponse<AuditLogResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("Administrative activity log")]
    [EndpointDescription("Who did what to which account and when, newest first. Optional Action filter (UserCreated, UserUpdated, RoleChanged, UserSuspended, UserDeactivated, UserRestored, UserDeleted, PasswordReset, ResetEmailSent).")]
    public async Task<IActionResult> GetAudit([FromQuery] AuditQuery query, CancellationToken ct) =>
        Ok(await adminService.GetAuditAsync(query, ct));
}