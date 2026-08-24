using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;
using PmfApi.Interface;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/medicine")]
public class MedicineController(IMedicinesService medicinesService) : ControllerBase
{
    
    [HttpGet("{id:int}", Name = nameof(GetmedicineById))]
    public async Task<IActionResult> GetmedicineById(int id, CancellationToken ct)
    {
        var medicine = await medicinesService.GetByIdAsync(id, ct);
        return medicine is not null ? Ok(medicine) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(MedicineRequest request, CancellationToken ct)
    {
        var result = await medicinesService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetmedicineById), new { id = result.Id }, result);
    }
      [HttpGet]
    public async Task<IActionResult> GetLocation([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await medicinesService.GetMedicineAsync(request,ct);
        return Ok(result);
    }

    
}