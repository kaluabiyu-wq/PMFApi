
using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/role")]

public class RoleController(IRoleService roleService) :ControllerBase
{
    [HttpPost]
  public async Task<IActionResult> CreateAsync(RoleRequest request, CancellationToken ct)    {
         var result = await roleService.CreateAsync(request,ct);
        return CreatedAtAction(nameof(GetByroleId), new { id = result.Id }, result);

    }
     [HttpGet("{id:int}", Name = nameof(GetByroleId))]
     public async Task<ActionResult<RoleResponse?>> GetByroleId(int id, CancellationToken ct)
    {
         var role = await roleService.GetByIdAsync(id, ct);
        return role is not null ? Ok(role) : NotFound();
 
    }

}



