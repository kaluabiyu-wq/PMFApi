using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;
using PmfApi.Interface;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/pharmaciesSchedule")]
public class PharmaciesScheduleController(IPharmaciesScheduleService pharmaciesScheduleService) : ControllerBase
{
    

    [HttpGet("{id:int}", Name = nameof(GetByPhramacyId))]
    public async Task<IActionResult> GetByPhramacyId(int pharmacyId,int id, CancellationToken ct)
    {
        var pharmacy = await pharmaciesScheduleService.GetByPhramacyIdAsync(pharmacyId, id,ct);
        return pharmacy is not null ? Ok(pharmacy) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(int pharmacyId,PharmaciesScheduleRequest request, CancellationToken ct)
    {
        var result = await pharmaciesScheduleService.CreateAsync(pharmacyId,request, ct);
        return CreatedAtAction(nameof(GetByPhramacyId), new { id = result.Id }, result);
    }
    
   
}
