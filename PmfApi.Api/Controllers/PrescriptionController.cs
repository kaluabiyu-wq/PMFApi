using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
[Tags("Prescriptions")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class PrescriptionController(
    IPrescriptionService prescriptionService,
    IOrderService orderService,
    IPharmaciesService pharmaciesService,
    IAuthorizationService authorizationService) : ControllerBase
{
    // ───────────── Patient side ─────────────

    [HttpPost("orders/{orderId:int}/prescriptions", Name = nameof(SubmitPrescription))]
    [ProducesResponseType(typeof(PrescriptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Submit a prescription for an order")]
    [EndpointDescription("Patient only (the order's owner), while the order is Pending and contains at least one prescription-only medicine. Creates a Pending prescription. Only one can await review at a time, and none can be added once one is Approved. A rejected prescription is replaced by submitting a new one. Someone else's order is reported as 404.")]
    public async Task<IActionResult> SubmitPrescription(int orderId, PrescriptionRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        var result = await prescriptionService.CreateAsync(orderId, userId, request, ct);
        return ToResult(result, p => CreatedAtAction(nameof(GetPrescriptionById), new { id = p.Id }, p));
    }

    [HttpGet("orders/{orderId:int}/prescriptions", Name = nameof(GetOrderPrescriptions))]
    [ProducesResponseType(typeof(List<PrescriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List an order's prescriptions")]
    [EndpointDescription("Newest first, including rejected attempts and their notes. Visible to the order's patient and to active staff of the order's pharmacy (or a SystemAdmin). Anyone else gets 404.")]
    public async Task<IActionResult> GetOrderPrescriptions(int orderId, CancellationToken ct)
    {
        var order = await orderService.GetByIdAsync(orderId, ct);
        if (order is null) return OrderNotFound(orderId);

        var isPatient = CurrentUserId == order.UserId;
        if (!isPatient && !await IsPharmacyStaffAsync(order.PharmacyId, ct))
            return OrderNotFound(orderId);

        return Ok(await prescriptionService.GetByOrderAsync(orderId, ct));
    }

    [HttpGet("prescriptions/{id:int}", Name = nameof(GetPrescriptionById))]
    [ProducesResponseType(typeof(PrescriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get one prescription")]
    [EndpointDescription("Visible to the patient who submitted it and to anyone who passes the PrescriptionVerification policy for it. Anyone else gets 404, so prescription ids cannot be probed.")]
    public async Task<IActionResult> GetPrescriptionById(int id, CancellationToken ct)
    {
        var entity = await prescriptionService.GetEntityByIdAsync(id, ct);
        if (entity is null) return PrescriptionNotFound(id);

        var isPatient = CurrentUserId == entity.Order.UserId;
        if (!isPatient && !await CanVerifyAsync(entity)) return PrescriptionNotFound(id);

        return Ok(await prescriptionService.GetByIdAsync(id, ct));
    }

    // ───────────── Pharmacy side (PrescriptionVerificationRequirement handler) ─────────────

    [HttpGet("pharmacies/{pharmacyId:int}/prescriptions", Name = nameof(GetPharmacyPrescriptions))]
    [ProducesResponseType(typeof(PagedResponse<PrescriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("A pharmacy's prescription verification queue")]
    [EndpointDescription("Prescriptions attached to orders placed with this pharmacy. Filter with ?status=Pending for the work queue (listed oldest first). Requires active staff of THIS pharmacy holding the PharmacyStaff or PharmacyAdmin role, or a SystemAdmin.")]
    public async Task<IActionResult> GetPharmacyPrescriptions(int pharmacyId, [FromQuery] PrescriptionListQuery query, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null)
            return NotFound(new ProblemDetails
            {
                Title = "Pharmacy not found",
                Detail = $"No pharmacy exists with id {pharmacyId}.",
                Status = StatusCodes.Status404NotFound,
            });

        var auth = await authorizationService.AuthorizeAsync(User, pharmacy, "PharmacyOwnerOrAdmin");
        if (!auth.Succeeded) return Forbid();

        return Ok(await prescriptionService.GetByPharmacyAsync(pharmacyId, query, ct));
    }

    [HttpPatch("prescriptions/{id:int}/review", Name = nameof(ReviewPrescription))]
    [ProducesResponseType(typeof(PrescriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Approve or reject a prescription")]
    [EndpointDescription("Requires the PrescriptionVerification policy: a SystemAdmin, or an active PharmacyStaff/PharmacyAdmin of the pharmacy the prescription's order was placed with. Sets VerificationStatus to Approved or Rejected (a note is required when rejecting) and records the caller from the token as the verifier. A prescription is decided once; deciding it again, or deciding one whose order is no longer Pending, returns 409. Approval is what allows an order containing prescription-only medicine to be confirmed.")]
    public async Task<IActionResult> ReviewPrescription(int id, PrescriptionReviewRequest request, CancellationToken ct)
    {
        var entity = await prescriptionService.GetEntityByIdAsync(id, ct);
        if (entity is null) return PrescriptionNotFound(id);

        if (!await CanVerifyAsync(entity))
        {
            
            return CurrentUserId == entity.Order.UserId ? Forbid() : PrescriptionNotFound(id);
        }

        if (CurrentUserId is not { } reviewerId) return Unauthorized();

        var result = await prescriptionService.ReviewAsync(id, reviewerId, request, ct);
        return ToResult(result, p => Ok(p));
    }

   

    private int? CurrentUserId =>
    int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),
       out var id) ? id : null;

   
    private async Task<bool> CanVerifyAsync(Prescription prescription) =>
        (await authorizationService.AuthorizeAsync(User, prescription, "PrescriptionVerification")).Succeeded;

    private async Task<bool> IsPharmacyStaffAsync(int pharmacyId, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        return pharmacy is not null
            && (await authorizationService.AuthorizeAsync(User, pharmacy, "PharmacyOwnerOrAdmin")).Succeeded;
    }

    private IActionResult OrderNotFound(int id) => NotFound(new ProblemDetails
    {
        Title = "Order not found",
        Detail = $"No order exists with id {id}.",
        Status = StatusCodes.Status404NotFound,
    });

    private IActionResult PrescriptionNotFound(int id) => NotFound(new ProblemDetails
    {
        Title = "Prescription not found",
        Detail = $"No prescription exists with id {id}.",
        Status = StatusCodes.Status404NotFound,
    });

    private IActionResult ToResult<T>(ServiceResult<T> result, Func<T,
     IActionResult> onSuccess) => result.OutCome switch
    {
        ServiceOutCome.Success => onSuccess(result.Value!),
        ServiceOutCome.NotFound => NotFound(
            new ProblemDetails {
                 Title = "Not found", 
                 Detail = result.Error, 
                 Status = StatusCodes.Status404NotFound
                  }),
        ServiceOutCome.Conflict => Conflict(
            new ProblemDetails { 
                Title = "Conflict", 
                Detail = result.Error,
                Status = StatusCodes.Status409Conflict 
                }),
        _ => BadRequest(new ProblemDetails { 
                Title = "Invalid request", 
                Detail = result.Error, 
                Status = StatusCodes.Status400BadRequest
                 }),
    };
}