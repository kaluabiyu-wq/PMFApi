using Microsoft.AspNetCore.Mvc;
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
}