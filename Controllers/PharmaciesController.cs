using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;
using PmfApi.Interface;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/pharmacies")]
[Tags("Pharmacies")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]

public class PharmaciesController(IPharmaciesService pharmaciesService) : ControllerBase
{
    
    [HttpGet("{id}", Name = nameof(GetBypharmacyId))]
    [ProducesResponseType(typeof(PharmacyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a pharmacy by ID")]
    [EndpointDescription("Returns pharmacy details, including verification and reliability status. Returns 404 if no pharmacy exists with that ID.")]
    public async Task<IActionResult> GetBypharmacyId(int id, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetByIdAsync(id, ct);
        return pharmacy is not null ? Ok(pharmacy) : NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(PharmacyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Register a pharmacy")]
    [EndpointDescription("Creates a new pharmacy record, including its licence, location, and initial verification state.")]
    public async Task<IActionResult> Create(PharmacyRequest request, CancellationToken ct)
    {
        var result = await pharmaciesService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetBypharmacyId), new { id = result.Id }, result);
    }

      [HttpGet]
       [ProducesResponseType(typeof(PagedResponse<PharmacyResponse>), StatusCodes.Status200OK)]
       [EndpointSummary("List pharmacy with pagination")]
       [EndpointDescription("Returns a paginated ")]
    public async Task<IActionResult> GetPharmacy([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await pharmaciesService.GetPharmacyAsync(request,ct);
        return Ok(result);
    }

   
}
