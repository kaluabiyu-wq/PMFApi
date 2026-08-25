using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;
using PmfApi.Interface;
using PmfApi.Entities;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/location")]
[Tags("Locations")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]

public class LocationController(ILocationService locationService) : ControllerBase
{
  

    [HttpGet("{id:int}", Name = nameof(GetByCoordinate))]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a location by ID and coordinate")]
    [EndpointDescription("Returns a single location record, resolved by its ID and the coordinate supplied on the request. Returns 404 if no matching location exists.")]
  
    public async Task<ActionResult<LocationResponse?>> GetByCoordinate(int id, Coordinate coordinate,CancellationToken ct)
    {
        var location = await locationService.GetByCoordinateAsync(id,coordinate, ct);
        return location is not null ? Ok(location) : NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Create a location")]
    [EndpointDescription("Creates a sub-city/woreda location record used to anchor pharmacies and searches to a place in Addis Ababa.")]
    public async Task<IActionResult> CreateAsync(LocationRequest request, CancellationToken ct)
    {
        var result = await locationService.CreateAsync(request,ct);
        return CreatedAtAction(nameof(GetByCoordinate ), new { id = result.Id }, result);
    }
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<LocationResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("List locations with pagination")]
    [EndpointDescription("Returns a paginated list of location records. PageSize is capped by PagedRequest.")]
    public async Task<IActionResult> GetLocation([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await locationService.GetLocationAsync(request,ct);
        return Ok(result);
    }

    
}