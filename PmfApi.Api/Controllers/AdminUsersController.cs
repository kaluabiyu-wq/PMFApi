using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Exceptions;
using PmfApi.Application.Interfaces;

namespace PmfApi.Api.Controllers;

/// <summary>Account management for system administrators. Every route needs the SysAdmin role.</summary>
[ApiController]
[Authorize(Roles = "SysAdmin")]
[Route("api/admin/users")]
[Tags("Admin - Users")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AdminUsersController(IAdminService adminService) : ControllerBase
{
    private int ActorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet(Name = nameof(ListUsers))]
    [ProducesResponseType(typeof(PagedResponse<AdminUserResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("Search and page through accounts")]
    [EndpointDescription("Filters: Search (name or email), RoleId, Status (Active, Suspended, Inactive, Deleted; omit for every account that is not deleted). OrderBy: FullName, Email, LastLoginAt or CreatedAt.")]
    public async Task<IActionResult> ListUsers([FromQuery] AdminUserQuery query, CancellationToken ct) =>
        Ok(await adminService.ListUsersAsync(query, ct));

    [HttpGet("{id:int}", Name = nameof(GetUser))]
    [ProducesResponseType(typeof(AdminUserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get one account with its activity counts")]
    public async Task<IActionResult> GetUser(int id, CancellationToken ct)
    {
        var user = await adminService.GetUserAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost(Name = nameof(CreateUser))]
    [ProducesResponseType(typeof(AdminUserCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Create an account")]
    [EndpointDescription("Creates an account with any role. If Password is omitted a one-time temporary password is generated and returned once. SendResetEmail also emails the person a link to choose their own.")]
    public async Task<IActionResult> CreateUser(AdminUserCreateRequest request, CancellationToken ct)
    {
        try
        {
            var created = await adminService.CreateUserAsync(ActorId, request, ct);
            return CreatedAtAction(nameof(GetUser), new { id = created.Detail.User.Id }, created);
        }
        catch (DuplicateEmailException) { return Problem409("Email already registered", "Another account already uses this email address."); }
        catch (RoleNotFoundException) { return Problem400("Unknown role", "The chosen role does not exist."); }
        catch (LocationNotFoundException) { return Problem400("Unknown location", "The chosen location does not exist."); }
    }

    [HttpPut("{id:int}", Name = nameof(UpdateUser))]
    [ProducesResponseType(typeof(AdminUserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Edit name and email")]
    public async Task<IActionResult> UpdateUser(int id, AdminUserUpdateRequest request, CancellationToken ct)
    {
        try
        {
            var user = await adminService.UpdateUserAsync(ActorId, id, request, ct);
            return user is null ? NotFound() : Ok(user);
        }
        catch (DuplicateEmailException) { return Problem409("Email already registered", "Another account already uses this email address."); }
    }

    [HttpPut("{id:int}/role", Name = nameof(ChangeRole))]
    [ProducesResponseType(typeof(AdminUserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Assign or revoke a role")]
    [EndpointDescription("Replaces the account's role. Refused for your own account and for the last active administrator, so nobody can lock the system out of administration.")]
    public async Task<IActionResult> ChangeRole(int id, AdminRoleChangeRequest request, CancellationToken ct)
    {
        try
        {
            var user = await adminService.ChangeRoleAsync(ActorId, id, request.RoleId, ct);
            return user is null ? NotFound() : Ok(user);
        }
        catch (RoleNotFoundException) { return Problem400("Unknown role", "The chosen role does not exist."); }
        catch (AdminRuleException ex) { return Problem409("Not allowed", ex.Message); }
    }

    [HttpPut("{id:int}/status", Name = nameof(SetUserStatus))]
    [ProducesResponseType(typeof(AdminUserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Restore, suspend or deactivate an account")]
    [EndpointDescription("Status Active restores a suspended or inactive account; Suspended (with an optional Reason) blocks sign-in but keeps everything; Inactive closes it. Takes effect on the person's next request. Refused for your own account and for the last active administrator.")]
    public async Task<IActionResult> SetUserStatus(int id, AdminStatusRequest request, CancellationToken ct)
    {
        try
        {
            var user = await adminService.SetStatusAsync(ActorId, id, request.Status, request.Reason, ct);
            return user is null ? NotFound() : Ok(user);
        }
        catch (AdminRuleException ex) { return Problem409("Not allowed", ex.Message); }
    }

    [HttpDelete("{id:int}", Name = nameof(DeleteUser))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Delete an account")]
    [EndpointDescription("The body must repeat the account's email as confirmation. Personal details are anonymised and personal-only data removed; orders, reviews and prescriptions are retained. Refused while the account has pending or confirmed orders, for yourself, and for the last active administrator.")]
    public async Task<IActionResult> DeleteUser(int id, AdminDeleteRequest request, CancellationToken ct)
    {
        try
        {
            return await adminService.DeleteUserAsync(ActorId, id, request.ConfirmEmail, ct) ? NoContent() : NotFound();
        }
        catch (AdminRuleException ex) { return Problem409("Not allowed", ex.Message); }
    }

    [HttpPost("{id:int}/reset-password", Name = nameof(ResetPassword))]
    [ProducesResponseType(typeof(TemporaryPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Set a temporary password")]
    [EndpointDescription("Generates a random password, stores only its hash, and returns it once. Pass it to the person over a secure channel; they should change it after signing in. Responses are not cached.")]
    public async Task<IActionResult> ResetPassword(int id, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var password = await adminService.ResetPasswordAsync(ActorId, id, ct);
        return password is null ? NotFound() : Ok(new TemporaryPasswordResponse(password));
    }

    [HttpPost("{id:int}/send-reset-email", Name = nameof(SendResetEmail))]
    [ProducesResponseType(typeof(SendResetEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Email a password-reset link")]
    [EndpointDescription("Sends a one-hour, single-use link. EmailSent is false when no mail server is configured (Email:Host and Email:From).")]
    public async Task<IActionResult> SendResetEmail(int id, CancellationToken ct)
    {
        var sent = await adminService.SendResetEmailAsync(ActorId, id, ct);
        if (sent is null) return NotFound();

        return Ok(new SendResetEmailResponse(sent.Value, sent.Value
            ? "A reset link was emailed to the user."
            : "The email could not be sent. Check the Email settings, or set a temporary password instead."));
    }

    private ObjectResult Problem409(string title, string detail) =>
        Conflict(new ProblemDetails { Title = title, Detail = detail, Status = StatusCodes.Status409Conflict });

    private ObjectResult Problem400(string title, string detail) =>
        BadRequest(new ProblemDetails { Title = title, Detail = detail, Status = StatusCodes.Status400BadRequest });
}