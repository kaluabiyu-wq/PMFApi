using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Exceptions;
using PmfApi.Application.Interfaces;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Auth")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AuthController(IAuthService authService
, IPharmacyRegistrationService registrationService) : ControllerBase
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
        var result = await authService.LoginAsync(request, ct);

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
}

