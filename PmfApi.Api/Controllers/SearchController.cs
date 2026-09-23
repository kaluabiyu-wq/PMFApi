using Microsoft.AspNetCore.Mvc;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace PmfApi.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/user/{userId:int}/search")]
[Tags("Search")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class SearchController(ISearchService searchService) : ControllerBase
{
    [HttpPost]
     [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Run and log a medicine search for a user")]
    [EndpointDescription("Executes a location-aware medicine search on behalf of the given user and persists the search along with its result set.")]
    public async Task<IActionResult> CreateAsync(int userId, SearchRequest request, CancellationToken ct)
    {
        var result = await searchService.CreateAsync(userId, request, ct);
        return CreatedAtAction(nameof(GetBySearch), new { userId, id = result.Id }, result);
    }

    [HttpGet("{id:int}", Name = nameof(GetBySearch))]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a past search by ID")]
    [EndpointDescription("Returns a previously logged search and its results for the given user. Returns 404 if no matching search exists.")]
    public async Task<ActionResult<SearchResponse?>> GetBySearch(int userId, int id, CancellationToken ct)
    {
        var search = await searchService.GetByIdAsync(id, userId, ct);
        return search is not null ? Ok(search) : NotFound();
    }
}