
using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;

namespace PmfApi.Controllers;





[ApiController]
[Route("api/user")]
[Tags("Users")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]

public class UserController(IUserService userService) :ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Register a user")]
    [EndpointDescription("Creates a new PMF user account.")]
  public async Task<IActionResult> CreateAsync(UserRequest request, CancellationToken ct)    {
         var result = await userService.CreateAsync(request,ct);
        return CreatedAtAction(nameof(GetByEmail), new { id = result.Id }, result);

    }
     [HttpGet("{id}", Name = nameof(GetByEmail))]
     [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
     [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
     [EndpointSummary("Get a user by email")]
     [EndpointDescription("Returns a single user record. Returns 404 if no user matches.")]
     public async Task<ActionResult<UserResponse?>> GetByEmail(string email, CancellationToken ct)
    {
         var user = await userService.GetByeEmailAsync(email, ct);
        return user is not null ? Ok(user) : NotFound();
 
    }
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("List users with pagination")]
    [EndpointDescription("Returns a paginated list of users. PageSize is capped by PagedRequest.")]
    public async Task<IActionResult> GetLocation([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await userService.GetUserAsync(request,ct);
        return Ok(result);
    }

}



