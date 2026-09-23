
using Microsoft.AspNetCore.Mvc;
using PmfApi.Dto;
using PmfApi.Interface;

namespace PmfApi.Controllers;

[ApiController]
[Route("api/user/{userId:int}/search")]

public class SearchController(ISearchService searchService):ControllerBase
{

   [HttpPost]
  public async Task<IActionResult> CreateAsync(int userId,SearchRequest request, CancellationToken ct)    {
         var result = await searchService.CreateAsync(userId,request,ct);
        return CreatedAtAction(nameof(GetBySearch), new {userId, id = result.Id }, result);

    }
     [HttpGet("{id}", Name = nameof(GetBySearch))]
     public async Task<ActionResult<SearchResponse?>> GetBySearch(int id, int userId, CancellationToken ct)
    {
         var search = await searchService.GetByIdAsync(id,userId, ct);
        return search is not null ? Ok(search) : NotFound();
 
    }  



}