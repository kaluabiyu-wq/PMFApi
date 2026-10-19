using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmfApi.Application.Dtos;
using PmfApi.Application.Exceptions;
using PmfApi.Application.Interfaces;

namespace PmfApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/account")]
[Tags("Account")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AccountController(IAccountService accountService) : ControllerBase
{
    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet(Name = nameof(GetMyAccount))]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get my account")]
    [EndpointDescription("The signed-in user's own details: name, email, role, chosen location, and counts of their orders (open and total), favourites and reviews. A user can only ever read their own account.")]
    public async Task<IActionResult> GetMyAccount(CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        var account = await accountService.GetAsync(userId, ct);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPut(Name = nameof(UpdateMyAccount))]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Edit my account")]
    [EndpointDescription("Changes the signed-in user's name, email and, optionally, location (send the id of an existing location). Returns 409 if another account already uses the email.")]
    public async Task<IActionResult> UpdateMyAccount(AccountUpdateRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        try
        {
            var account = await accountService.UpdateAsync(userId, request, ct);
            return account is null ? NotFound() : Ok(account);
        }
        catch (DuplicateEmailException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Email already registered",
                Detail = "Another account already uses this email address.",
                Status = StatusCodes.Status409Conflict,
            });
        }
        catch (LocationNotFoundException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Location not found",
                Detail = "The chosen location does not exist.",
                Status = StatusCodes.Status400BadRequest,
            });
        }
    }

    [HttpPut("password", Name = nameof(ChangeMyPassword))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Change my password")]
    [EndpointDescription("Requires the current password. The new password must be at least 8 characters. A wrong current password returns 400.")]
    public async Task<IActionResult> ChangeMyPassword(ChangePasswordRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        return await accountService.ChangePasswordAsync(userId, request, ct) switch
        {
            true => NoContent(),
            false => BadRequest(new ProblemDetails
            {
                Title = "Wrong password",
                Detail = "The current password is incorrect.",
                Status = StatusCodes.Status400BadRequest,
            }),
            null => NotFound(),
        };
    }

    [HttpDelete(Name = nameof(DeleteMyAccount))]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Delete my account")]
    [EndpointDescription("Patients only. Needs the account password in the body. Deactivates the account (it can no longer sign in) and keeps order and review history. Refused with 409 while the patient still has Pending or Confirmed orders. Pharmacy accounts are closed by removing the pharmacy instead.")]
    public async Task<IActionResult> DeleteMyAccount(DeleteAccountRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();

        return await accountService.DeleteAsync(userId, request.Password, ct) switch
        {
            AccountDeleteOutcome.Deleted => NoContent(),
            AccountDeleteOutcome.WrongPassword => BadRequest(new ProblemDetails
            {
                Title = "Wrong password",
                Detail = "The password is incorrect, so the account was not deleted.",
                Status = StatusCodes.Status400BadRequest,
            }),
            AccountDeleteOutcome.HasOpenOrders => Conflict(new ProblemDetails
            {
                Title = "Open orders",
                Detail = "Cancel or collect your pending and confirmed orders before deleting your account.",
                Status = StatusCodes.Status409Conflict,
            }),
            _ => NotFound(),
        };
    }
}