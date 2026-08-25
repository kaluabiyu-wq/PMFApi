using Microsoft.AspNetCore.Mvc;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/pharmaciesSchedule")]
[Tags("Pharmacy Schedules")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]

public class PharmaciesScheduleController(IPharmaciesScheduleService pharmaciesScheduleService) : ControllerBase
{
    

    [HttpGet("{id:int}", Name = nameof(GetByPhramacyId))]
    [ProducesResponseType(typeof(PharmacyScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a pharmacy's opening-hours schedule entry")]
    [EndpointDescription("Returns a single schedule row for the given pharmacy. Returns 404 if the pharmacy or the schedule entry does not exist.")]
    public async Task<IActionResult> GetByPhramacyId(int pharmacyId,int id, CancellationToken ct)
    {
        var pharmacy = await pharmaciesScheduleService.GetByPhramacyIdAsync(pharmacyId, id,ct);
        return pharmacy is not null ? Ok(pharmacy) : NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(PharmacyScheduleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Add a schedule entry for a pharmacy")]
    [EndpointDescription("Creates an opening-hours entry for the given pharmacy. Returns 404 if the pharmacy does not exist.")]

    public async Task<IActionResult> CreateAsync(int pharmacyId,PharmaciesScheduleRequest request, CancellationToken ct)
    {
        var result = await pharmaciesScheduleService.CreateAsync(pharmacyId,request, ct);
        return CreatedAtAction(nameof(GetByPhramacyId), new { id = result.Id }, result);
    }
    
   
}
