
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmfApi.Data;


namespace PmfApi.Controllers;

[ApiController]
[Route("api/inventory")]


// public record UpdatePriceRequest(decimal NewPrice);

public class InventoryController(IInventoryService inventoryService,PmfDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var inventories = await inventoryService.GetAllAsync();
        return Ok(inventories);
    }
    
    [HttpGet("id")]
    public async Task<IActionResult> GetById(string id)
    {
        var inventories = await inventoryService.GetByIdAsync(id);
        return inventories is not null ? Ok(inventories): NotFound();
    }
    //  [HttpPatch("{id:int}/price")]
    // public async Task<IActionResult> UpdatePrice(int id,[FromBody] UpdatePriceRequest request, CancellationToken cancellationToken)
    // {
    //     var item = await context.Inventories.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    //     if (item is null) return NotFound();
    //     item.Price = request.NewPrice;
    //     item.LastUpdatedAt = DateTime.UtcNow;

    //     try
    //     {
    //         await context.SaveChangesAsync(cancellationToken);
    //         return Ok(new { item.Id, item.Price });
    //     }
    //     catch (DbUpdateConcurrencyException)
    //     {
    //         return Conflict(new { Message = "This inventory row was modified by someone else. Reload and retry." });
    //     }
    // }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateInventoryRequest request)
    {
        var inventories = await inventoryService.CreateAsync(
            request.PharmacyId,
            request.MedicineId,
            request.Price
   );

    return CreatedAtAction(
        nameof(GetById),
        new { id = inventories.Id},inventories);
    }
    [HttpDelete("id")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await inventoryService.DeleteAsync(id);

        return deleted ? NoContent () :NotFound();
    }
    

   public record CreateInventoryRequest(
     string MedicineId,
    string PharmacyId,
    decimal Price
   
);


}