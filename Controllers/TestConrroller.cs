using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmfApi.Data;
using PmfApi.Entities;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestController(PmfDbContext context) : ControllerBase
{
    [HttpGet("deferred/pharmacies")]
    public IActionResult TestPharmacies()
    {
        Console.WriteLine("\n>> :Building the query object (no database contact)...");
        var query = context.Pharmacies.Where(a=>a.ReliablityScore >= 56m);
        Console.WriteLine(">> Appending a sorting clause...");
        var orderQuery = query.OrderBy (a=>a.Name);
        Console.WriteLine(">>> Materlizing query into List");
        var result = orderQuery.ToList();
         
         Console.WriteLine(">>> Materaliation finished List.\n");
         return Ok(result);
    }
    [HttpGet("deferred/inventory")]
    public IActionResult TestInventory()
    {
        Console.WriteLine("\n>> :Building the query object ...");
        var query = context.Inventories.Where(i=>i.Price < 80m);
        Console.WriteLine(">> Appending a sorting clause...");
        var orderQuery = query.OrderBy (i=>i.PharmacyId);
        Console.WriteLine(">>> Materlizing query into List");
        var result = orderQuery.ToList();
         
         Console.WriteLine(">>> Materaliation finished List.\n");
         return Ok(result);
    }

   
    private static bool IsBudgetFriednly(decimal price) => price <=80m;

     [HttpGet("SQL-translation-fail")]
    public IActionResult TeranslaationFail()
    {
        Console.WriteLine("\n>> Running non-translatale query");
        try
        {
            var items = context.Inventories.Where(i=>IsBudgetFriednly(i.Price))
              .ToList();
              return Ok(items);
         }
         catch (Exception ex)
        {
            Console.WriteLine($">> Exeption Caught: {ex.Message}\n");
            return BadRequest(new {Message = ex.Message});
        }

    }

   
    [HttpGet("n-plus-one")]
    public async Task<IActionResult> DemonstrateNPlusOne(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n>>> N+1 DEMO: fetching all pharmacies (query #1)...");

        var pharmacies = await context.Pharmacies
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var report = new List<object>();
        var queryCount = 1; 

        foreach (var ph in pharmacies)
        {
                 var count = await context.Inventories
                .AsNoTracking()
                .CountAsync(i => i.PharmacyId == ph.Id && i.Status == "Fresh", cancellationToken);

            report.Add(new { ph.Name, InStockCount = count });
        }

     
        return Ok(report);
    }
    [HttpGet("patient-search")]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var query = context.Medicines.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(m => m.GenericName.Contains(q) || (m.BrandName != null && m.BrandName.Contains(q)));
        }

        var results = await query
            .Select(m => new { m.Id, m.GenericName, m.BrandName, m.Category })
            .ToListAsync(cancellationToken);

        return Ok(results);
    }

     [HttpGet("admin/all")]
    public async Task<IActionResult> AdminSearch(CancellationToken cancellationToken)
    {
        var results = await context.Medicines
            .IgnoreQueryFilters()
            .Select(m => new { m.Id, m.GenericName, m.BrandName, m.Category, m.IsActive })
            .ToListAsync(cancellationToken);

        return Ok(results);
    }
}
