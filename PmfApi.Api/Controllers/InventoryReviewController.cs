using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Api.Controllers;


[ApiController]
[Route("api/inventory/submissions")]
[Tags("Inventory review")]
[Produces("application/json")]
[Authorize(Roles = "PharmacyAdmin,SystemAdmin")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class InventoryReviewController(IInventoryReviewService reviewService) : ControllerBase
{
    [HttpGet(Name = nameof(GetSubmissions))]
    [ProducesResponseType(typeof(IReadOnlyList<InventorySubmissionResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("List inventory submissions for review")]
    [EndpointDescription("Returns inventory submissions across all pharmacies. status filters by Pending (default), Approved or Rejected; pass status=all for everything.")]
    public async Task<IActionResult> GetSubmissions([FromQuery] string? status, CancellationToken ct)
    {
        var filter = string.IsNullOrWhiteSpace(status) ? InventoryApprovalStatus.Pending
            : status.Equals("all", StringComparison.OrdinalIgnoreCase) ? null
            : InventoryApprovalStatus.IsValid(status) ? status
            : null;

        return Ok(await reviewService.GetSubmissionsAsync(filter, ct));
    }

    [HttpPost("{id:int}/approve", Name = nameof(ApproveSubmission))]
    [ProducesResponseType(typeof(InventorySubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Approve a pending inventory submission")]
    [EndpointDescription("Makes the pharmacy's stock row live (visible to patients). Only Pending rows can be approved.")]
    public async Task<IActionResult> ApproveSubmission(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Forbid();

        var result = await reviewService.ApproveAsync(id, userId, ct);
        return ToActionResult(result, id);
    }

    [HttpPost("{id:int}/reject", Name = nameof(RejectSubmission))]
    [ProducesResponseType(typeof(InventorySubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Reject a pending inventory submission")]
    [EndpointDescription("Rejects the submission with a reason the pharmacy will see. Only Pending rows can be rejected.")]
    public async Task<IActionResult> RejectSubmission(int id, RejectInventoryRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            ModelState.AddModelError(nameof(request.Reason), "A rejection reason is required.");
            return ValidationProblem(ModelState);
        }

        if (!TryGetUserId(out var userId)) return Forbid();

        var result = await reviewService.RejectAsync(id, userId, request.Reason, ct);
        return ToActionResult(result, id);
    }

    private IActionResult ToActionResult(ReviewResult result, int id) => result.Outcome switch
    {
        ReviewOutcome.Done => Ok(result.Item),
        ReviewOutcome.NotFound => NotFound(new ProblemDetails
        {
            Title = "Inventory not found",
            Detail = $"No inventory submission exists with id {id}.",
            Status = StatusCodes.Status404NotFound,
        }),
        _ => Conflict(new ProblemDetails
        {
            Title = "Already reviewed",
            Detail = "Only pending submissions can be approved or rejected.",
            Status = StatusCodes.Status409Conflict,
        }),
    };

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
