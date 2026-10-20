using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Exceptions;
using PmfApi.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Auth")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AuthController(IAuthService authService
, IPharmacyRegistrationService registrationService,PmfDbContext context,
IUserService userService, IAccountRecoveryService recoveryService) : ControllerBase
 {
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Log in")]
    [EndpointDescription("Verifies email + password against the stored password hash and, " +
        "on success, issues a bearer JWT carrying the user's ID as a NameIdentifier claim " +
        "and their role name as a Role claim.")]
    public async Task<IActionResult> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        LoginResponse? result;
        try
        {
            result = await authService.LoginAsync(request, ct);
        }
        catch (AccountSuspendedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Account suspended",
                Status = StatusCodes.Status403Forbidden,
                Detail = string.IsNullOrWhiteSpace(ex.Message)
                    ? "This account is suspended. Contact an administrator."
                    : $"This account is suspended: {ex.Message}",
            });
        }

        if (result is null)
        {
            
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "The email or password is incorrect.",
            });
        }

        return Ok(result);
    }

    [HttpPost("register-patient")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Sign up as a patient")]
    [EndpointDescription("Public sign-up. The role is always Patient, whatever the caller sends, so nobody can create a privileged account here.")]
    public async Task<IActionResult> RegisterPatient(RegisterPatientRequest request, CancellationToken ct)
    {
        var roleId = await context.Roles.Where(r => r.Name == "Patient").Select(r => r.Id).FirstOrDefaultAsync(ct);
        if (roleId == 0)
            return Problem(title: "Patient role is missing", statusCode: StatusCodes.Status500InternalServerError);

        var email = request.Email.Trim();
        if (await context.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict(new ProblemDetails { Title = "Email already registered", Status = StatusCodes.Status409Conflict });

        if (!await context.Locations.AnyAsync(l => l.Id == request.LocationId, ct))
            return BadRequest(new ProblemDetails { Title = "Unknown LocationId", Status = StatusCodes.Status400BadRequest });

        await userService.CreateAsync(new UserRequest
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Password = request.Password,
            LocationId = request.LocationId,
            RoleId = roleId,
        }, ct);

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [EndpointSummary("Request a password-reset email")]
    [EndpointDescription("Always answers 202, whether or not the email belongs to an account, so it cannot be used to discover who is registered.")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await recoveryService.RequestResetAsync(request.Email, ct);
        return Accepted();
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Choose a new password with an emailed link")]
    [EndpointDescription("Needs the email and token from the reset link. A token works once and expires after one hour.")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        var ok = await recoveryService.ResetAsync(request.Email, request.Token, request.NewPassword, ct);
        return ok
            ? NoContent()
            : BadRequest(new ProblemDetails
            {
                Title = "Reset link invalid or expired",
                Detail = "Ask for a new password-reset email and try again.",
                Status = StatusCodes.Status400BadRequest,
            });
    }

 [HttpPost("register-pharmacy")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(RegisterPharmacyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Register a pharmacy")]
    [EndpointDescription("Creates the owner account (Pharmacy role), an unverified pharmacy, the owner's " +
        "PharmacyStaff link and three Pending verification documents in a single call.")]
    public async Task<IActionResult> RegisterPharmacy([FromForm] RegisterPharmacyRequest request, CancellationToken ct)
    {
        try
        {
            var result = await registrationService.RegisterAsync(request, ct);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (DuplicateEmailException)
        {
            return Conflict(new ProblemDetails { Title = "Email already registered", Status = 409 });
        }
        catch (DuplicateLicenseException)
        {
            return Conflict(new ProblemDetails { Title = "License number already registered", Status = 409 });
        }
        catch (LocationNotFoundException)
        {
            return BadRequest(new ProblemDetails { Title = "Unknown LocationId", Status = 400 });
        }
        catch (InvalidUploadException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid upload", Detail = ex.Message, Status = 400 });
        }
    }

[Authorize]
[HttpGet("me")]
public async Task<ActionResult<UserProfileDto>> Me(CancellationToken ct)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var profile = await context.Users
        .Where(u => u.Id == userId)
        .Select(u => new UserProfileDto(
            u.FullName,
            u.LocationId,
             u.Location != null ? u.Location.Label : null,
    
            context.PharmacyStaff
                .Where(s => s.UserId == u.Id && s.IsActive && s.Pharmacy.IsActive)
                .OrderBy(s => s.AssignedAt)
                .Select(s => (int?)s.PharmacyId)
                .FirstOrDefault()))
        .FirstOrDefaultAsync(ct);

    return profile is null ? NotFound() : Ok(profile);
}


}

public record UserProfileDto(string FullName, int? LocationId, string? LocationLabel,int? PharmacyId);