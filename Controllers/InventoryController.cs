using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;
using PmfApi.Interface;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/pharmacies/{pharmacyId:int}/inventory")]
[Tags("Inventory")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]

public class InventoryController(
    IInventoryService inventoryService,
    IPharmaciesService pharmaciesService,
    IMedicinesService medicinesService) : ControllerBase
{
    
    [HttpGet(Name = nameof(GetInventoryByMedicine))]
    [ProducesResponseType(typeof(InventoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a pharmacy's stock record for one medicine")]
    [EndpointDescription("Returns the inventory row for the given medicineId at this pharmacy. Returns 404 if the pharmacy or the medicine does not exist, or if the pharmacy has no stock record for that medicine.")]
 
   
    public async Task<IActionResult> GetInventoryByMedicine(int pharmacyId, [FromQuery] int medicineId, CancellationToken ct)
    {
        var notFound = await CheckParentsExistAsync(pharmacyId, medicineId, ct);
        if (notFound is not null) return notFound;

        var inventory = await inventoryService.GetByPharmacyAndMedicineAsync(pharmacyId, medicineId, ct);
        return inventory is not null ? Ok(inventory) : NotFound();
    }

    [HttpGet("{id:int}", Name = nameof(GetInventoryById))]
    [ProducesResponseType(typeof(InventoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get an inventory record by ID")]
    [EndpointDescription("Returns a single inventory row by its own ID, scoped to the parent pharmacy. Returns 404 if the pharmacy does not exist or the inventory row is not found.")]
 
    public async Task<IActionResult> GetInventoryById(int pharmacyId, int id, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var inventory = await inventoryService.GetByIdAsync(id, ct);
        return inventory is not null ? Ok(inventory) : NotFound();
    }


    [HttpGet("medicines", Name = nameof(GetMedicinesByPharmacy))]
    [ProducesResponseType(typeof(IReadOnlyList<PharmacyMedicineDetail>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List every medicine a pharmacy stocks")]
    [EndpointDescription("Returns the full medicine catalogue this pharmacy currently carries, with price, dosage form, and freshness status per line. Returns 404 if the pharmacy does not exist.")]
    public async Task<IActionResult> GetMedicinesByPharmacy(int pharmacyId, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var medicines = await inventoryService.GetMedicineDetailsByPharmacyAsync(pharmacyId, ct);
        return Ok(medicines);
    }

    [HttpPost(Name = nameof(Create))]
    [ProducesResponseType(typeof(InventoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Add a stock record for a medicine at a pharmacy")]
    [EndpointDescription("Creates an inventory row linking the pharmacy to a medicine, with price and stock status. Returns 404 if the pharmacy or the medicine referenced by MedicineId does not exist.")]
    public async Task<IActionResult> Create(int pharmacyId, InventoryRequest request, CancellationToken ct)
    {
        var notFound = await CheckParentsExistAsync(pharmacyId, request.MedicineId, ct);
        if (notFound is not null) return notFound;

        var result = await inventoryService.CreateAsync(pharmacyId, request, ct);
        return CreatedAtAction(nameof(GetInventoryById), new { pharmacyId, id = result.Id }, result);
    }

    private async Task<IActionResult?> CheckPharmacyExistsAsync(int pharmacyId, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetByIdAsync(pharmacyId, ct);
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