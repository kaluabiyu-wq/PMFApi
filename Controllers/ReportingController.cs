using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using PmfApi.Data;
using PmfApi.Entities;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/report")]
public class ReportController(PmfDbContext context) : ControllerBase
{
    [HttpGet("verified")]
    public async Task<IActionResult> Verify()
    {
        var count = await context.Pharmacies
        .Where(p=>p.IsVerified && p.IsActive)
        .CountAsync();
        return Ok(count);
    }
    [HttpGet("distinctmedicine")]
    public async Task<IActionResult> Distinctmedicine()
    {
        var distinctmedicine = await context.Pharmacies
        .Select(p=> new {p.Name,ItemCount =p.Inventories.Count})
        .OrderByDescending( x => x.ItemCount)
        .ToListAsync();

        return Ok(distinctmedicine);
    }
    [HttpGet("averageprice")]
    public async Task<IActionResult> AveragePrice()
    {
        var average = await context.Inventories
        .GroupBy(i=>i.Medicine.GenericName)
        .Select(g=> new {Medicine = g.Key, AveragePrice =g.Average(i => i.Price)})
        .ToListAsync();

        return Ok(average);
    }
    [HttpGet("medicinelist")]
    public async Task<IActionResult>  Medicinelist()
    {
        var list = await context.Medicines
         .Where(m => !m.Inventories.Any())
        .Select(m => m.GenericName)
        .ToListAsync();
        return Ok(list);

    }
    [HttpGet("paged/pharmacies")]
    public async Task<IActionResult> PharmaciesGroup(int pagesize = 20,
    int pageNumber = 1,
    CancellationToken ct = default)
    {
        var page = await context.Pharmacies
        .OrderBy( p => p.Name)
        .Skip((pageNumber -1 ) * pagesize)
        .Take(pagesize)
        .ToListAsync(ct);

        return Ok(page);

    }
    [HttpGet("paged/medicinies")]
    public async Task<IActionResult> MediciniesGroup(int pagesize = 20,
    int pageNumber = 1,
    CancellationToken ct = default)
    {
        var page = await context.Medicines
        .OrderBy( p => p.GenericName)
        .Skip((pageNumber -1 ) * pagesize)
        .Take(pagesize)
        .ToListAsync(ct);
        return Ok(page);

    }
    [HttpGet("Paginate/inventories")]
    public async Task<IActionResult> GetTopCourses(
    CancellationToken ct = default)
    {
 var pharmacies = await context.Inventories
 .GroupBy(i => i.PharmacyId)
 .Select(g => new
 {
     
     Medicine = g.Key,
     MedicineCount = g.Count(),
     ReliablityScore = g.Average(p=>p.Pharmacy.ReliablityScore),
     lowPrice = g.Min(i => i.Price)
    
     }).OrderByDescending(s => s.MedicineCount)
 .Take(5)
  .ToListAsync(ct);

  return Ok(pharmacies);

    }



}