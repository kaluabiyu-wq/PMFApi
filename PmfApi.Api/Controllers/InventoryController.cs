using Microsoft.AspNetCore.Mvc;
using PmfApi.Infrastructure.Persistence;


using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/pharmacies/{pharmacyId:int}/inventory")]
[Tags("Inventory")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]

public class InventoryController(
    IInventoryService inventoryService,
    IPharmaciesService pharmaciesService,
    IMedicinesService medicinesService,
    IAuthorizationService authorizationService) : ControllerBase
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
    [HttpPut("{id:int}", Name = nameof(updateInventory))]
    [ProducesResponseType(typeof(InventoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Edit a pharmacy's stock record")]
    [EndpointDescription("Updates Price and/or Status on one inventory row. Only fields present in the body change; omitted fields keep their stored value. Always refreshes LastUpdatedAt and records the caller as the updating user. A price change also writes an InventoryHistory row holding the old price. Requires active staff of this pharmacy (or a SystemAdmin). Returns 404 if the pharmacy does not exist or the row is not in this pharmacy.")]
    public async Task<IActionResult> updateInventory(int pharmacyId, int id, InventoryUpdateRequest request
    ,CancellationToken ct)
    {
        var (failure, userId) = await AuthorizePharmacyWriteAsync(pharmacyId,ct);
        if(failure is not null) return failure;

        var result = await inventoryService.UpdateAsync(pharmacyId,id, request,userId,ct);
        return result is not null ? Ok(result) : NotFound();
    }
    [HttpDelete("{id:int}", Name = nameof(DeleteInventory))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Remove a stock record from a pharmacy")]
    [EndpointDescription("Permanently deletes the inventory row, and with it that row's InventoryHistory entries (database cascade). Requires active staff of this pharmacy (or a SystemAdmin). Returns 404 if the pharmacy does not exist or the row is not in this pharmacy.")]
    public async Task<IActionResult> DeleteInventory(int pharmacyId, int id,CancellationToken ct)
    {
        var (failure,_) = await AuthorizePharmacyWriteAsync(pharmacyId,ct);
        if(failure is not null) return failure ;
        var deleted = await inventoryService.DeleteAsync(pharmacyId,id,ct);
        return deleted ? NoContent() : NotFound();
    }

    private async Task<(IActionResult? Failure, int UserId)> AuthorizePharmacyWriteAsync(int pharmacyId, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null)
        {
            return (NotFound(new ProblemDetails
            {
                Title = "Pharmacy not found",
                Detail = $"No pharmacy exists with id {pharmacyId}.",
                Status = StatusCodes.Status404NotFound,
            }), 0);
        }

        var auth = await authorizationService.AuthorizeAsync(User, pharmacy, "PharmacyOwnerOrAdmin");
        if (!auth.Succeeded) return (Forbid(), 0);

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return (Forbid(), 0);

        return (null, userId);
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

    [HttpGet("/api/inventory/medicines/{medicineId:int}/pharmacies",
    Name = nameof(GetPharmaciesByMedicine))]
[ProducesResponseType(typeof(MedicinePharmacyInventoryResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[EndpointSummary("Get all pharmacies that stock a medicine")]
[EndpointDescription(
    "Returns every pharmacy that has an inventory record for the specified medicine, including price, stock status, and last updated date.")]
public async Task<IActionResult> GetPharmaciesByMedicine(
    int medicineId,
    CancellationToken ct)
{
    var medicine = await medicinesService.GetByIdAsync(medicineId, ct);

    if (medicine is null)
    {
        return NotFound(new ProblemDetails
        {
            Title = "Medicine not found",
            Detail = $"No medicine exists with id {medicineId}.",
            Status = StatusCodes.Status404NotFound
        });
    }

    var result = await inventoryService.GetPharmaciesByMedicineAsync(
        medicineId,
        ct);

    return result is not null
        ? Ok(result)
        : NotFound(new ProblemDetails
        {
            Title = "No inventory found",
            Detail = $"No pharmacy has an inventory record for medicine {medicineId}.",
            Status = StatusCodes.Status404NotFound
        });
}
[HttpGet("/api/inventory/medicines/pharmacies",
    Name = nameof(GetAllMedicinesWithPharmacies))]
[ProducesResponseType(
    typeof(List<MedicinePharmacyInventoryResponse>),
    StatusCodes.Status200OK)]
[EndpointSummary("Get all medicines with all pharmacies")]
[EndpointDescription(
    "Returns every medicine in inventory together with every pharmacy that has an inventory record for that medicine.")]
public async Task<IActionResult> GetAllMedicinesWithPharmacies(CancellationToken ct)
 {
    var inventory =
        await inventoryService.GetAllMedicinesWithPharmaciesAsync(ct);

    return Ok(inventory);
  }
}