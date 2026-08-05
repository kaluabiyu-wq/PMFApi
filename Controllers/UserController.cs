
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/user")]


public class UserController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var user = await userService.GetAllAsync();
        return Ok(user);
    }
    
    [HttpGet("id")]
    public async Task<IActionResult> GetById(string id)
    {
        var users = await userService.GetByIdAsync(id);
        return users is not null ? Ok(users): NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request)
    {
        var user = await userService.CreateAsync(
          
          request.FullName,
          request.Email,
          request.Password,
          request.RoleID,
          request.LocationID,
          request.IsActive,
          request.CreatedAt
    );

    return CreatedAtAction(
        nameof(GetById),
        new { id = user.Id},user);
    }
    [HttpDelete("id")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await userService.DeleteAsync(id);
        return deleted ? NoContent () :NotFound();
    }
    

   public record CreateUserRequest(
     string FullName,
    decimal Email,
    decimal Password,
    string RoleID,
    string LocationID,
    bool IsActive,
    bool CreatedAt
   
);

}

