using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;
using PmfApi.Interface;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/medicine")]
[Tags("Medicines")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]

public class MedicineController(IMedicinesService medicinesService) : ControllerBase
{
    
    [HttpGet("{id:int}", Name = nameof(GetmedicineById))]
    [ProducesResponseType(typeof(MedicineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a medicine by ID")]
    [EndpointDescription("Returns catalogue details for a single medicine. Returns 404 if the medicine does not exist.")]
    public async Task<IActionResult> GetmedicineById(int id, CancellationToken ct)
    {
        var medicine = await medicinesService.GetByIdAsync(id, ct);
        return medicine is not null ? Ok(medicine) : NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(MedicineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Add a medicine to the catalogue")]
    [EndpointDescription("Creates a new medicine catalogue entry (generic name, brand name, category, dosage form).")]
    public async Task<IActionResult> Create(MedicineRequest request, CancellationToken ct)
    {
        var result = await medicinesService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetmedicineById), new { id = result.Id }, result);
    }
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<MedicineResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("List medicines with pagination")]
    [EndpointDescription("Returns a paginated list of catalogue medicines. PageSize is capped by PagedRequest.")]
    public async Task<IActionResult> GetLocation([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await medicinesService.GetMedicineAsync(request,ct);
        return Ok(result);
    }

    
}