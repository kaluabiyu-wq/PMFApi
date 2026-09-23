using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;
using PmfApi.Interface;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/pharmacies")]
public class PharmaciesController(IPharmaciesService pharmaciesService) : ControllerBase
{
    
    [HttpGet("{id}", Name = nameof(GetBylicence))]
    public async Task<IActionResult> GetBylicence(string id, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetBylicenceAsync(id, ct);
        return pharmacy is not null ? Ok(pharmacy) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(PharmacyRequest request, CancellationToken ct)
    {
        var result = await pharmaciesService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetBylicence), new { id = result.Id }, result);
    }
      [HttpGet]
    public async Task<IActionResult> GetLocation([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await pharmaciesService.GetPharmacyAsync(request,ct);
        return Ok(result);
    }

   
}
