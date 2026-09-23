
using Microsoft.AspNetCore.Mvc;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/role")]
[Tags("Roles")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]

public class RoleController(IRoleService roleService) :ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Create a role")]
    [EndpointDescription("Creates a new user role (e.g. patient, pharmacist, admin) used to gate PMF permissions.")]
  public async Task<IActionResult> CreateAsync(RoleRequest request, CancellationToken ct)    {
         var result = await roleService.CreateAsync(request,ct);
        return CreatedAtAction(nameof(GetByroleId), new { id = result.Id }, result);

    }
     [HttpGet("{id:int}", Name = nameof(GetByroleId))]
     [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
     [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
     [EndpointSummary("Get a role by ID")]
     [EndpointDescription("Returns a single role by its ID. Returns 404 if no role exists with that ID.")]
     public async Task<ActionResult<RoleResponse?>> GetByroleId(int id, CancellationToken ct)
    {
         var role = await roleService.GetByIdAsync(id, ct);
        return role is not null ? Ok(role) : NotFound();
 
    }

}



