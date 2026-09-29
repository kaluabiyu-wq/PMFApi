using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/admin/pharmacies")]
[Tags("Pharmacy Admin")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class PharmacyAdminController(IPharmacyAdminService pharmacyAdminService,
 IPharmaciesService pharmaciesService,
 IAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet(Name = nameof(GetPharmacies))]
    [ProducesResponseType(typeof(PagedResponse<PharmacyAdminSummaryResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("List pharmacies for the admin dashboard")]
    [EndpointDescription("Returns a paginated, filterable list of pharmacies with reliability score, freshness status, active staff count, and pending document count. Filter by IsVerified, IsActive, and Freshness (\"Fresh\"/\"Stale\"); sort with OrderBy=Name|LicenceNumber|ReliabilityScore.")]
    public async Task<IActionResult> GetPharmacies([FromQuery] PharmacyAdminQuery query, CancellationToken ct)
    {
        var result = await pharmacyAdminService.GetAllAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}/profile", Name = nameof(GetProfile))]
    [ProducesResponseType(typeof(PharmacyAdminProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a pharmacy's full admin profile")]
    [EndpointDescription("Returns the pharmacy's core details plus reliability, freshness, staff counts, document review counts, and review/rating summary. Returns 404 if no pharmacy exists with that ID.")]
    public async Task<IActionResult> GetProfile(int id, CancellationToken ct)
    {
        var profile = await pharmacyAdminService.GetProfileAsync(id, ct);
        return profile is not null ? Ok(profile) : NotFound();
    }

    [HttpPut("{id:int}", Name = nameof(UpdatePharmacy))]
    [ProducesResponseType(typeof(PharmacyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Edit a pharmacy's profile")]
    [EndpointDescription("Updates name, licence number, location, phone, email, and freshness threshold. Only the fields present in the request body are changed; omitted fields keep their stored value. For email, send an empty string to clear it. Returns 404 if no pharmacy exists with that ID.")]
        public async Task<IActionResult> UpdatePharmacy(int id, PharmacyUpdateRequest request, CancellationToken ct)
    {
        var result = await pharmacyAdminService.UpdateAsync(id, request, ct);
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpPatch("{id:int}/status", Name = nameof(SetStatus))]
    [ProducesResponseType(typeof(PharmacyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Verify or suspend a pharmacy")]
    [EndpointDescription("Sets IsVerified and IsActive together, with an optional Reason for the audit log. Returns 404 if no pharmacy exists with that ID.")]
    public async Task<IActionResult> SetStatus(int id, PharmacyStatusUpdateRequest request, CancellationToken ct)
    {
        var result = await pharmacyAdminService.SetStatusAsync(id, request, ct);
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpDelete("{id:int}", Name = nameof(DeletePharmacy))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Remove a pharmacy")]
    [EndpointDescription("Soft-deletes the pharmacy (IsActive=false) so staff, documents, inventory, and review history are preserved rather than destroyed. Returns 404 if no pharmacy exists with that ID.")]
    public async Task<IActionResult> DeletePharmacy(int id, CancellationToken ct)
    {
          var pharmacy = await pharmaciesService.GetEntityByIdAsync(id, ct);
        if (pharmacy is null) return NotFound();
 
        var authResult = await authorizationService.AuthorizeAsync(User, pharmacy, "PharmacyOwnerOrAdmin");
        if (!authResult.Succeeded) return Forbid();
        var deleted = await pharmacyAdminService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}