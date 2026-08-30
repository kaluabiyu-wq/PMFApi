using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/users/{userId:int}/favorites")]
[Tags("Favorites")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class FavoriteController(
    IFavoriteService favoriteService,
    IUserService userService,
    IPharmaciesService pharmaciesService) : ControllerBase
{
    [HttpGet(Name = nameof(GetFavoritesByUser))]
    [ProducesResponseType(typeof(IReadOnlyList<FavoriteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List a user's favorite pharmacies")]
    [EndpointDescription("Returns every pharmacy this user has favorited, most recent first. Returns 404 if the user does not exist.")]
    public async Task<IActionResult> GetFavoritesByUser(int userId, CancellationToken ct)
    {
        var notFound = await CheckUserExistsAsync(userId, ct);
        if (notFound is not null) return notFound;

        var favorites = await favoriteService.GetByUserAsync(userId, ct);
        return Ok(favorites);
    }

    [HttpGet("{id:int}", Name = nameof(GetFavoriteById))]
    [ProducesResponseType(typeof(FavoriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a single favorite")]
    [EndpointDescription("Returns a single favorite scoped to the parent user. Returns 404 if the user or the favorite does not exist.")]
    public async Task<IActionResult> GetFavoriteById(int userId, int id, CancellationToken ct)
    {
        var notFound = await CheckUserExistsAsync(userId, ct);
        if (notFound is not null) return notFound;

        var favorite = await favoriteService.GetByUserIdAsync(userId, id, ct);
        return favorite is not null ? Ok(favorite) : NotFound();
    }

    [HttpGet("check/{pharmacyId:int}", Name = nameof(CheckFavorite))]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Check whether a pharmacy is one of the user's favorites")]
    [EndpointDescription("Returns true/false for whether this user has favorited the given pharmacy — useful for boosting it in that user's own search results. Returns 404 if the user does not exist.")]
    public async Task<IActionResult> CheckFavorite(int userId, int pharmacyId, CancellationToken ct)
    {
        var notFound = await CheckUserExistsAsync(userId, ct);
        if (notFound is not null) return notFound;

        var isFavorited = await favoriteService.IsFavoritedAsync(userId, pharmacyId, ct);
        return Ok(isFavorited);
    }

    [HttpPost(Name = nameof(CreateFavorite))]
    [ProducesResponseType(typeof(FavoriteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Favorite a pharmacy")]
    [EndpointDescription("Marks a pharmacy as trusted by this user. Returns 404 if the user or the pharmacy does not exist.")]
    public async Task<IActionResult> CreateFavorite(int userId, FavoriteRequest request, CancellationToken ct)
    {
        var notFound = await CheckUserExistsAsync(userId, ct);
        if (notFound is not null) return notFound;

        var pharmacy = await pharmaciesService.GetByIdAsync(request.PharmacyId, ct);
        if (pharmacy is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Pharmacy not found",
                Detail = $"No pharmacy exists with id {request.PharmacyId}.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var result = await favoriteService.CreateAsync(userId, request, ct);
        return CreatedAtAction(nameof(GetFavoriteById), new { userId, id = result.Id }, result);
    }

    [HttpDelete("{id:int}", Name = nameof(DeleteFavorite))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Unfavorite a pharmacy")]
    [EndpointDescription("Removes the favorite. Returns 404 if the user or the favorite does not exist.")]
    public async Task<IActionResult> DeleteFavorite(int userId, int id, CancellationToken ct)
    {
        var notFound = await CheckUserExistsAsync(userId, ct);
        if (notFound is not null) return notFound;

        var deleted = await favoriteService.DeleteAsync(userId, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    private async Task<IActionResult?> CheckUserExistsAsync(int userId, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Detail = $"No user exists with id {userId}.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        return null;
    }
}
