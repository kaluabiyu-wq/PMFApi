using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;

namespace PmfApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/alerts")]
[Tags("Alerts")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AlertController(IAlertService alertService) : ControllerBase
{
    [HttpGet(Name = nameof(GetMyAlerts))]
    [ProducesResponseType(typeof(PagedResponse<AlertResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("List my alerts")]
    [EndpointDescription("The signed-in user's alerts, newest first. Use ?isRead=false for the unread inbox. Each alert names the row it is about (referenceTable + referenceId: Orders, Prescriptions or PharmacyDocuments) so a client can link straight to it.")]
    public async Task<IActionResult> GetMyAlerts([FromQuery] AlertListQuery query, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        return Ok(await alertService.GetMineAsync(userId, query, ct));
    }

    [HttpGet("unread-count", Name = nameof(GetUnreadAlertCount))]
    [ProducesResponseType(typeof(AlertCountResponse), StatusCodes.Status200OK)]
    [EndpointSummary("Count my unread alerts")]
    [EndpointDescription("A single number for the notification badge, served from a small partial index so it is cheap enough to poll.")]
    public async Task<IActionResult> GetUnreadAlertCount(CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        return Ok(new AlertCountResponse(await alertService.CountUnreadAsync(userId, ct)));
    }

    [HttpPatch("{id:int}/read", Name = nameof(MarkAlertRead))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Mark one alert as read")]
    [EndpointDescription("Idempotent: marking an already-read alert still returns 204. An alert that does not exist or belongs to someone else returns 404.")]
    public async Task<IActionResult> MarkAlertRead(int id, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        return await alertService.MarkReadAsync(userId, id, ct)
            ? NoContent()
            : NotFound(new ProblemDetails
            {
                Title = "Alert not found",
                Detail = $"No alert exists with id {id}.",
                Status = StatusCodes.Status404NotFound,
            });
    }

    [HttpPost("read-all", Name = nameof(MarkAllAlertsRead))]
    [ProducesResponseType(typeof(AlertCountResponse), StatusCodes.Status200OK)]
    [EndpointSummary("Mark all my alerts as read")]
    [EndpointDescription("Returns how many alerts changed from unread to read.")]
    public async Task<IActionResult> MarkAllAlertsRead(CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        return Ok(new AlertCountResponse(await alertService.MarkAllReadAsync(userId, ct)));
    }

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}