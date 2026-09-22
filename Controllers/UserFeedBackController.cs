
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/userfeedback")]


public class UserFeedBackController(IUserFeedBackService userFeedBackService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userFeedBack = await userFeedBackService.GetAllAsync();
        return Ok(userFeedBack);
    }
    
    [HttpGet("id")]
    public async Task<IActionResult> GetById(string id)
    {
        var userFeedBack = await userFeedBackService.GetByIdAsync(id);
        return userFeedBack is not null ? Ok(userFeedBack): NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserFeedBackRequest request)
    {
        var userFeedBack = await userFeedBackService.CreateAsync(
          
          request.UserId,
          request.InventoryId,
          request.PharmacyId,
          request.WasMedicineAvailable,
          request.SubmittedAt,
          request.Comments
    );

    return CreatedAtAction(
        nameof(GetById),
        new { id = userFeedBack.Id},userFeedBack);
    }
    [HttpDelete("id")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await userFeedBackService.DeleteAsync(id);
        return deleted ? NoContent () :NotFound();
    }
    

   public record CreateUserFeedBackRequest(
    string UserId,
   string InventoryId,
  string PharmacyId,
  bool WasMedicineAvailable,
  DateTime SubmittedAt,
  string Comments
   
);

}

