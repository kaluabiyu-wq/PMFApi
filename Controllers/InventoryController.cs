using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;
using PmfApi.Interface;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/pharmacies/{pharmacyId:int}/medicines/{medicineId:int}/inventory")]
public class InventoryController(IInventoryService inventoryService) : ControllerBase
{
  

    [HttpGet("{id:int}", Name = nameof(GetInventoryById))]
    public async Task<IActionResult> GetInventoryById(int pharmacyid,int medicineId,int id, CancellationToken ct)
    {
        var inventory = await inventoryService.GetByIdAsync(pharmacyid,medicineId,id, ct);
        return inventory is not null ? Ok(inventory) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(int pharmacyId, int medicineId,InventoryRequest request, CancellationToken ct)
    {
        var result = await inventoryService.CreateAsync(pharmacyId,medicineId,request, ct);
        return CreatedAtAction(nameof(GetInventoryById ), new { pharmacyId,medicineId,id = result.Id }, result);
    }

    
}