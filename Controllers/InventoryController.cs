using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;
using PmfApi.Interface;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/pharmacies/{pharmacyId:int}/inventory")]
public class InventoryController(
    IInventoryService inventoryService,
    IPharmaciesService pharmaciesService,
    IMedicinesService medicinesService) : ControllerBase
{
    [HttpGet(Name = nameof(GetInventoryByMedicine))]
    public async Task<IActionResult> GetInventoryByMedicine(int pharmacyId, [FromQuery] int medicineId, CancellationToken ct)
    {
        var notFound = await CheckParentsExistAsync(pharmacyId, medicineId, ct);
        if (notFound is not null) return notFound;

        var inventory = await inventoryService.GetByPharmacyAndMedicineAsync(pharmacyId, medicineId, ct);
        return inventory is not null ? Ok(inventory) : NotFound();
    }

    [HttpGet("{id:int}", Name = nameof(GetInventoryById))]
    public async Task<IActionResult> GetInventoryById(int pharmacyId, int id, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var inventory = await inventoryService.GetByIdAsync(id, ct);
        return inventory is not null ? Ok(inventory) : NotFound();
    }


    [HttpGet("medicines", Name = nameof(GetMedicinesByPharmacy))]
    public async Task<IActionResult> GetMedicinesByPharmacy(int pharmacyId, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var medicines = await inventoryService.GetMedicineDetailsByPharmacyAsync(pharmacyId, ct);
        return Ok(medicines);
    }

    [HttpPost(Name = nameof(Create))]
    public async Task<IActionResult> Create(int pharmacyId, InventoryRequest request, CancellationToken ct)
    {
        var notFound = await CheckParentsExistAsync(pharmacyId, request.MedicineId, ct);
        if (notFound is not null) return notFound;

        var result = await inventoryService.CreateAsync(pharmacyId, request, ct);
        return CreatedAtAction(nameof(GetInventoryById), new { pharmacyId, id = result.Id }, result);
    }

    private async Task<IActionResult?> CheckPharmacyExistsAsync(int pharmacyId, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetBylicenceAsync(pharmacyId, ct);
        if (pharmacy is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Pharmacy not found",
                Detail = $"No pharmacy exists with id {pharmacyId}.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        return null;
    }

    private async Task<IActionResult?> CheckParentsExistAsync(int pharmacyId, int medicineId, CancellationToken ct)
    {
        var medicine = await medicinesService.GetByIdAsync(medicineId, ct);
        if (medicine is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Medicine not found",
                Detail = $"No medicine exists with id {medicineId}.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        return await CheckPharmacyExistsAsync(pharmacyId, ct);
    }
}