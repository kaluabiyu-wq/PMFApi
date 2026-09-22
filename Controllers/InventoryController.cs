
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/inventory")]


public class InventoryController(IInventoryService inventoryService) : ControllerBase
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