using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Interfaces;
using PmfApi.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using PmfApi.Api.Authorization;

namespace PmfApi.Api.Controllers;

[ApiController]
[Route("api/users/{userId:int}/feedback")]
[Tags("User Feedback")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class UserFeedBackController(IUserFeedBackService userFeedBackService,
IPharmaciesService pharmaciesService,
    IAuthorizationService authorizationService
) : ControllerBase
{
    [HttpGet("{inventoryId:int}", Name = nameof(GetUserFeedbackById))]
    [ProducesResponseType(typeof(UserFeedBackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a user's feedback on a medicine's availability")]
    [EndpointDescription("Returns the feedback this user submitted for the given inventory row. Returns 404 if no such feedback exists.")]
    public async Task<IActionResult> GetUserFeedbackById(int userId, int inventoryId, CancellationToken ct)
    {
        var feedback = await userFeedBackService.GetByUserIdAsync(userId, inventoryId, ct);
        return feedback is not null ? Ok(feedback) : NotFound();
    }

    [HttpPost(Name = nameof(CreateUserFeedback))]
    [ProducesResponseType(typeof(UserFeedBackResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Submit feedback on a medicine's availability")]
    [EndpointDescription("Records whether the medicine was actually available at the pharmacy, as reported by this user.")]
    public async Task<IActionResult> CreateUserFeedback(int userId, UserFeedBackRequest request, CancellationToken ct)
    {
        var result = await userFeedBackService.CreateAsync(userId, request, ct);
        return CreatedAtAction(nameof(GetUserFeedbackById), new { userId, inventoryId = result.InventoryId }, result);
    }

    [Authorize]
    [HttpGet("/api/pharmacies/{pharmacyId:int}/feedback", Name = nameof(GetFeedbackForPharmacy))]
    [ProducesResponseType(typeof(PagedResponse<PharmacyFeedbackResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List the availability feedback left on this pharmacy's stock")]
    [EndpointDescription("Active staff of this pharmacy (or a SysAdmin) only. Returns feedback on this pharmacy's own inventory rows, newest first by default.")]
    public async Task<IActionResult> GetFeedbackForPharmacy(
        int pharmacyId, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var pharmacy = await pharmaciesService.GetEntityByIdAsync(pharmacyId, ct);
        if (pharmacy is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Pharmacy not found",
                Detail = $"No pharmacy exists with id {pharmacyId}.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var auth = await authorizationService.AuthorizeAsync(User, pharmacy, "PharmacyOwnerOrAdmin");
        if (!auth.Succeeded) return Forbid();

        return Ok(await userFeedBackService.GetByPharmacyAsync(pharmacyId, request, ct));
    }

[Authorize(Roles = RoleNames.SystemAdmin)]
[HttpGet("/api/feedback", Name = nameof(GetAllFeedback))]
[ProducesResponseType(typeof(PagedResponse<UserFeedBackResponse>), StatusCodes.Status200OK)]
[EndpointSummary("List all feedback across every user and pharmacy")]
public async Task<IActionResult> GetAllFeedback([FromQuery] PagedRequest request, CancellationToken ct)
{
    var feedback = await userFeedBackService.GetAllAsync(request, ct);
    return Ok(feedback);
}
}