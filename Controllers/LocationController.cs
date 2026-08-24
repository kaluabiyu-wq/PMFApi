using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;
using PmfApi.Interface;
using PmfApi.Entities;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/location")]
public class LocationController(ILocationService locationService) : ControllerBase
{
  

    [HttpGet("{id:int}", Name = nameof(GetByCoordinate))]
    public async Task<ActionResult<LocationResponse?>> GetByCoordinate(int id, Coordinate coordinate,CancellationToken ct)
    {
        var location = await locationService.GetByCoordinateAsync(id,coordinate, ct);
        return location is not null ? Ok(location) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(LocationRequest request, CancellationToken ct)
    {
        var result = await locationService.CreateAsync(request,ct);
        return CreatedAtAction(nameof(GetByCoordinate ), new { id = result.Id }, result);
    }
    [HttpGet]
    public async Task<IActionResult> GetLocation([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await locationService.GetLocationAsync(request,ct);
        return Ok(result);
    }

    
}