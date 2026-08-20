
using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;

namespace PmfApi.Controllers;





[ApiController]
[Route("api/user")]

public class UserController(IUserService userService) :ControllerBase
{
    [HttpPost]
  public async Task<IActionResult> CreateAsync(UserRequest request, CancellationToken ct)    {
         var result = await userService.CreateAsync(request,ct);
        return CreatedAtAction(nameof(GetByEmail), new { id = result.Id }, result);

    }
     [HttpGet("{id}", Name = nameof(GetByEmail))]
     public async Task<ActionResult<UserResponse?>> GetByEmail(string email, CancellationToken ct)
    {
         var user = await userService.GetByeEmailAsync(email, ct);
        return user is not null ? Ok(user) : NotFound();
 
    }

}



