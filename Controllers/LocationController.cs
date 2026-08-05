
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/location")]


public class LocationController(ILocationService locationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var location = await locationService.GetAllAsync();
        return Ok(location);
    }
    
    [HttpGet("id")]
    public async Task<IActionResult> GetById(string id)
    {
        var location = await locationService.GetByIdAsync(id);
        return location is not null ? Ok(location): NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateLocationRequest request)
    {
        var location = await locationService.CreateAsync(
           request.Label,
           request.Latitude,
           request.Longitude,
           request.Subcity,
           request.Woreda,
           request.City
        
    );

    return CreatedAtAction(
        nameof(GetById),
        new { id = location.Id},location);
    }
    [HttpDelete("id")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await locationService.DeleteAsync(id);
        return deleted ? NoContent () :NotFound();
    }
    

   public record CreateLocationRequest(
     string Label,
    decimal Latitude,
    decimal Longitude,
    string Subcity,
    string Woreda,
    string City
   
);

}

