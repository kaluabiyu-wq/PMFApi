using Microsoft.AspNetCore.SignalR;
using PmfApi.Application.Abstractions;

namespace PmfApi.Api.Hubs;

public class SignalRPmfHubClient(IHubContext<PmfHub> hub, ILogger<SignalRPmfHubClient> logger) : IPmfHubClient
{
    public Task InventoryStatusChangedAsync(InventoryStatusChangedMessage m, CancellationToken ct = default) =>
        SafeAsync(() => hub.Clients
            .Groups(PmfGroups.Pharmacy(m.PharmacyId), PmfGroups.Medicine(m.MedicineId))
            .SendAsync(PmfHubEvents.InventoryStatusChanged, m, ct));

    public Task OrderCreatedAsync(OrderCreatedMessage m, CancellationToken ct = default) =>
        SafeAsync(() => hub.Clients
            .Groups(PmfGroups.Pharmacy(m.PharmacyId), PmfGroups.User(m.PatientUserId.ToString()))
            .SendAsync(PmfHubEvents.OrderCreated, m, ct));

    public Task OrderStatusChangedAsync(OrderStatusChangedMessage m, CancellationToken ct = default) =>
        SafeAsync(() => hub.Clients
            .Groups(PmfGroups.Pharmacy(m.PharmacyId), PmfGroups.User(m.PatientUserId.ToString()))
            .SendAsync(PmfHubEvents.OrderStatusChanged, m, ct));

    public Task PrescriptionVerifiedAsync(PrescriptionVerifiedMessage m, CancellationToken ct = default) =>
        SafeAsync(() => hub.Clients
            .Groups(PmfGroups.Pharmacy(m.PharmacyId), PmfGroups.User(m.PatientUserId.ToString()))
            .SendAsync(PmfHubEvents.PrescriptionVerified, m, ct));

    // A failed push must never fail the HTTP request that triggered it
    private async Task SafeAsync(Func<Task> send)
    {
        try { await send(); }
        catch (Exception ex) { logger.LogWarning(ex, "PmfHub broadcast failed"); }
    }
}