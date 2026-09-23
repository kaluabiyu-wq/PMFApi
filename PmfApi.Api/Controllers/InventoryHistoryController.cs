using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/inventory/{inventoryId:int}/history")]
[Tags("Inventory History")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class InventoryHistoryController(
    IInventoryHistoryService inventoryHistoryService,
    IInventoryService inventoryService) : ControllerBase
{
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