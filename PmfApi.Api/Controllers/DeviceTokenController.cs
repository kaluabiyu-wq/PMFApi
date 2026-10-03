using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;

namespace PmfApi.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/device-tokens")]
[Tags("Device Tokens")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class DeviceTokenController(IDeviceTokenService deviceTokenService) : ControllerBase
{
    [HttpPut(Name = nameof(RegisterDevice))]
    [ProducesResponseType(typeof(DeviceTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Register or refresh this device for push notifications")]
    [EndpointDescription("Call after every sign-in and on every app launch, because the push provider can rotate a token at any time. Idempotent: registering the same token again just refreshes lastSeenAt. A token identifies a device installation, not a person, so if it was registered to a different user (a shared phone) it moves to the caller and the previous user stops receiving pushes there. A user keeps at most 10 devices; the least recently seen are dropped. The token is never returned.")]
    public async Task<IActionResult> RegisterDevice(DeviceTokenRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        var result = await deviceTokenService.RegisterAsync(userId, request, ct);

        return result.OutCome switch
        {
            ServiceOutCome.Success => Ok(result.Value),
            ServiceOutCome.Conflict => Conflict(new ProblemDetails { Title = "Conflict", Detail = result.Error, Status = StatusCodes.Status409Conflict }),
            _ => BadRequest(new ProblemDetails { Title = "Invalid request", Detail = result.Error, Status = StatusCodes.Status400BadRequest }),
        };
    }

    [HttpGet(Name = nameof(GetMyDevices))]
    [ProducesResponseType(typeof(List<DeviceTokenResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("List my registered devices")]
    [EndpointDescription("The caller's devices, most recently seen first: platform and timestamps only, never the token itself.")]
    public async Task<IActionResult> GetMyDevices(CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        return Ok(await deviceTokenService.GetMineAsync(userId, ct));
    }

    [HttpDelete("{id:int}", Name = nameof(RemoveDevice))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Remove one of my devices")]
    [EndpointDescription("Stops pushes to that device. A registration that does not exist or belongs to someone else returns 404.")]
    public async Task<IActionResult> RemoveDevice(int id, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        return await deviceTokenService.RemoveAsync(userId, id, ct)
            ? NoContent()
            : NotFound(new ProblemDetails
            {
                Title = "Device not found",
                Detail = $"No registered device exists with id {id}.",
                Status = StatusCodes.Status404NotFound,
            });
    }

    [HttpPost("unregister", Name = nameof(UnregisterDevice))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Unregister this device by its token (use on logout)")]
    [EndpointDescription("The API issues bearer tokens and has no logout endpoint, so a client should call this BEFORE it discards its JWT; otherwise the phone keeps receiving the previous user's alerts until someone else signs in on it. The token goes in the body, never the URL. Always 204, whether or not a registration was removed, so the endpoint cannot be used to discover which tokens exist.")]
    public async Task<IActionResult> UnregisterDevice(DeviceTokenUnregisterRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        await deviceTokenService.RemoveByTokenAsync(userId, request.PushToken, ct);
        return NoContent();
    }

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}