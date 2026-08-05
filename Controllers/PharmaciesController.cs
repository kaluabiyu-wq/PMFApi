
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/pharmacies")]


public class PharmaciesController(IPharmaciesService pharmaciesService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var pharmacies = await pharmaciesService.GetAllAsync();
        return Ok(pharmacies);
    }
    
    [HttpGet("id")]
    public async Task<IActionResult> GetById(string id)
    {
        var pharmacies = await pharmaciesService.GetByIdAsync(id);
        return pharmacies is not null ? Ok(pharmacies): NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePharmaciesRequest request)
    {
        var pharmacies = await pharmaciesService.CreateAsync(
    request.Name,
     request.LicenceNumber,
   request.PhoneNumber,
    request.Email,
    request.IsVerified,
    request.IsActive,
     request.ReliablityScore,
     request.FreshnessThreshold);

    return CreatedAtAction(
        nameof(GetById),
        new { id = pharmacies.Id},pharmacies);
    }
    [HttpDelete("id")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await pharmaciesService.DeleteAsync(id);

        return deleted ? NoContent () :NotFound();
    }
    

   public record CreatePharmaciesRequest(string Id,
    string Name,string LicenceNumber,
    int PhoneNumber,string Email,
    bool IsVerified,bool IsActive,
    decimal ReliablityScore,int FreshnessThreshold,
    DateTime LastinventoryUpdateAt,DateTime RegisteredAt
);


}