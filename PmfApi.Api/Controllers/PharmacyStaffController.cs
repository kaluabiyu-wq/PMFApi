using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/pharmacies/{pharmacyId:int}/staff")]
[Tags("Pharmacy Staff")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class PharmacyStaffController(
    IPharmacyStaffService pharmacyStaffService,
    IPharmaciesService pharmaciesService) : ControllerBase
{
    [HttpGet(Name = nameof(GetStaffByPharmacy))]
    [ProducesResponseType(typeof(IReadOnlyList<PharmacyStaffResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List the staff authorized for a pharmacy")]
    [EndpointDescription("Returns every staff assignment for the pharmacy, active or not. Returns 404 if the pharmacy does not exist.")]
    public async Task<IActionResult> GetStaffByPharmacy(int pharmacyId, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var staff = await pharmacyStaffService.GetByPharmacyAsync(pharmacyId, ct);
        return Ok(staff);
    }

    [HttpGet("{id:int}", Name = nameof(GetStaffById))]
    [ProducesResponseType(typeof(PharmacyStaffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a single staff assignment")]
    [EndpointDescription("Returns a single staff assignment scoped to the parent pharmacy. Returns 404 if the pharmacy or the assignment does not exist.")]
    public async Task<IActionResult> GetStaffById(int pharmacyId, int id, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var staff = await pharmacyStaffService.GetByPharmacyIdAsync(pharmacyId, id, ct);
        return staff is not null ? Ok(staff) : NotFound();
    }

    [HttpPost(Name = nameof(CreateStaff))]
    [ProducesResponseType(typeof(PharmacyStaffResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Authorize a user to represent a pharmacy")]
    [EndpointDescription("Creates a staff assignment linking a user to this pharmacy, with a position (e.g. Pharmacist, Cashier, Manager). Returns 404 if the pharmacy does not exist.")]
    public async Task<IActionResult> CreateStaff(int pharmacyId, PharmacyStaffRequest request, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var result = await pharmacyStaffService.CreateAsync(pharmacyId, request, ct);
        return CreatedAtAction(nameof(GetStaffById), new { pharmacyId, id = result.Id }, result);
    }

    [HttpPatch("{id:int}/deactivate", Name = nameof(Deactivate))]
    [ProducesResponseType(typeof(PharmacyStaffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Revoke a staff member's authorization")]
    [EndpointDescription("Sets IsActive to false for the given staff assignment, so the user is no longer authorized to update this pharmacy's inventory. Returns 404 if the pharmacy or the assignment does not exist.")]
    public async Task<IActionResult> Deactivate(int pharmacyId, int id, CancellationToken ct)
    {
        var notFound = await CheckPharmacyExistsAsync(pharmacyId, ct);
        if (notFound is not null) return notFound;

        var result = await pharmacyStaffService.DeactivateAsync(pharmacyId, id, ct);
        return result is not null ? Ok(result) : NotFound();
    }

    private async Task<IActionResult?> CheckPharmacyExistsAsync(int pharmacyId, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetByIdAsync(pharmacyId, ct);
        if (pharmacy is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Pharmacy not found",
                Detail = $"No pharmacy exists with id {pharmacyId}.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        return null;
    }
}
