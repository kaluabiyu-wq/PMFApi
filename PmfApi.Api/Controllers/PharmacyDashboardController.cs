using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;

namespace PmfApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pharmacies/{pharmacyId:int}/dashboard")]
[Tags("Pharmacy Dashboard")]
[Produces("application/json")]
public class PharmacyDashboardController(
    IPharmacyDashboardService dashboardService,
    IPharmaciesService pharmaciesService,
    IAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet(Name = nameof(GetDashboard))]
    [ProducesResponseType(typeof(PharmacyDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get the figures shown on a pharmacy's own dashboard")]
    public async Task<IActionResult> GetDashboard(int pharmacyId, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null) return NotFound();

        var auth = await authorizationService.AuthorizeAsync(User, pharmacy, "PharmacyOwnerOrAdmin");
        if (!auth.Succeeded) return Forbid();

        var result = await dashboardService.GetAsync(pharmacyId, ct);
        return result is null ? NotFound() : Ok(result);
    }
}