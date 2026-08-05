
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/inventoryhistory")]


public class InventoryHistoryController(IInventoryHistoryService inventoryHistoryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var inventoryHistories = await inventoryHistoryService.GetAllAsync();
        return Ok(inventoryHistories);
    }
    
    [HttpGet("id")]
    public async Task<IActionResult> GetById(string id)
    {
        var inventoryHistories = await inventoryHistoryService.GetByIdAsync(id);
        return inventoryHistories is not null ? Ok(inventoryHistories): NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateInventoryHistoryRequest request)
    {
        var inventoryHistories = await inventoryHistoryService.CreateAsync(
            request.InventoryId,
            request.PharmacyId,
            request.MedicineId,
            request.OldPrice
           
   );

    return CreatedAtAction(
        nameof(GetById),
        new { id = inventoryHistories.Id},inventoryHistories);
    }
    [HttpDelete("id")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await inventoryHistoryService.DeleteAsync(id);

        return deleted ? NoContent () :NotFound();
    }
    

   public record CreateInventoryHistoryRequest(
    string InventoryId,
    string MedicineId,
    string PharmacyId,
    decimal OldPrice
   
);


}