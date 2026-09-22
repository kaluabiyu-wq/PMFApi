
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/medicine")]


public class MedicineController(IMedicinesService medicinesService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var medicines = await medicinesService.GetAllAsync();
        return Ok(medicines);
    }
    
    [HttpGet("id")]
    public async Task<IActionResult> GetById(string id)
    {
        var medicines = await medicinesService.GetByIdAsync(id);
        return medicines is not null ? Ok(medicines): NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMedicineRequest request)
    {
        var medicine = await medicinesService.CreateAsync(
            request.Genericname,
            request.Brandname,
            request.Category,
            request.Dosegeform,
            request.Strength,
            request.Requeirsprescription,
            request.Isactive
    );

    return CreatedAtAction(
        nameof(GetById),
        new { id = medicine.Id},medicine);
    }
    [HttpDelete("id")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await medicinesService.DeleteAsync(id);
        return deleted ? NoContent () :NotFound();
    }
    

   public record CreateMedicineRequest(
    string Id,string Genericname,string Brandname,
    string Category,string Dosegeform,string Strength,
    bool Requeirsprescription,bool Isactive
);

}