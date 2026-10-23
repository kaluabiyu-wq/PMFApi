using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/inventory/{inventoryId:int}/history")]
[Tags("Inventory History")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class InventoryHistoryController(
    IInventoryHistoryService inventoryHistoryService,
    IInventoryService inventoryService,
    IPharmaciesService pharmaciesService,
    IAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet(Name = nameof(GetInventoryPriceChanges))]
    [ProducesResponseType(typeof(PagedResponse<InventoryPriceChangeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List the price history of an inventory row")]
    [EndpointDescription("Returns a page of price changes for the inventory row, newest first, each with the old price, the price it became, who changed it and when. Requires active staff of the owning pharmacy (or an admin). Returns 404 if the inventory row does not exist.")]
    public async Task<IActionResult> GetInventoryPriceChanges(
        int inventoryId, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var inventory = await inventoryService.GetByIdAsync(inventoryId, ct);
        if (inventory is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Inventory record not found",
                Detail = $"No inventory record exists with id {inventoryId}.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        // Authorize against the pharmacy that OWNS this row, not against anything the client sends.
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(inventory.PharmacyId, ct);
        if (pharmacy is null) return NotFound();

        var auth = await authorizationService.AuthorizeAsync(User, pharmacy, "PharmacyOwnerOrAdmin");
        if (!auth.Succeeded) return Forbid();

        return Ok(await inventoryHistoryService.GetPriceChangesAsync(inventoryId, request, ct));
    }

    [HttpGet("{id:int}", Name = nameof(GetInventoryHistoryById))]
    [ProducesResponseType(typeof(InventoryHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a single inventory history entry")]
    [EndpointDescription("Returns one price-change record for the given inventory row. Returns 404 if the inventory record or the history entry does not exist.")]
    public async Task<IActionResult> GetInventoryHistoryById(int inventoryId, int id, CancellationToken ct)
    {
        var notFound = await CheckInventoryExistsAsync(inventoryId, ct);
        if (notFound is not null) return notFound;

        var history = await inventoryHistoryService.GetByinventoryIdAsync(inventoryId, id, ct);
        return history is not null ? Ok(history) : NotFound();
    }

    [HttpPost(Name = nameof(CreateInventoryHistory))]
    [ProducesResponseType(typeof(InventoryHistoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Record a price change for an inventory row")]
    [EndpointDescription("Creates a history entry capturing the prior price before an inventory update. Returns 404 if the inventory record does not exist.")]
    public async Task<IActionResult> CreateInventoryHistory(int inventoryId, InventoryHistoryRequest request, CancellationToken ct)
    {
        var notFound = await CheckInventoryExistsAsync(inventoryId, ct);
        if (notFound is not null) return notFound;

        var result = await inventoryHistoryService.CreateAsync(inventoryId, request, ct);
        return CreatedAtAction(nameof(GetInventoryHistoryById), new { inventoryId, id = result.Id }, result);
    }

    private async Task<IActionResult?> CheckInventoryExistsAsync(int inventoryId, CancellationToken ct)
    {
        var inventory = await inventoryService.GetByIdAsync(inventoryId, ct);
        if (inventory is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Inventory record not found",
                Detail = $"No inventory record exists with id {inventoryId}.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        return null;
    }
}