using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PmfApi.Application.Abstractions;

namespace PmfApi.Api.Hubs;

[Authorize]
public class PmfHub(IPharmacyMembershipService membership, ILogger<PmfHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, PmfGroups.User(userId));

        foreach (var role in Context.User!.FindAll(ClaimTypes.Role))
            await Groups.AddToGroupAsync(Context.ConnectionId, PmfGroups.Role(role.Value));

        // Pharmacy groups come from the DB, never from the client
        var isPharmacySide = Context.User.IsInRole("Pharmacy")
            || Context.User.IsInRole("PharmacyStaff")
            || Context.User.IsInRole("PharmacyAdmin");

        if (isPharmacySide)
        {
            var pharmacyIds = await membership.GetPharmacyIdsForUserAsync(userId, Context.ConnectionAborted);
            foreach (var id in pharmacyIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, PmfGroups.Pharmacy(id));
        }

        logger.LogInformation("PmfHub connected {User} {Conn}", userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    // Opt-in for patients watching a medicine's availability
    public Task WatchMedicine(int medicineId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, PmfGroups.Medicine(medicineId));

    public Task UnwatchMedicine(int medicineId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, PmfGroups.Medicine(medicineId));
}