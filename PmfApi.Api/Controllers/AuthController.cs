using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Auth")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AuthController(IAuthService authService) : ControllerBase
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
}