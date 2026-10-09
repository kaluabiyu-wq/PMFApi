namespace PmfApi.Application.Abstractions;

public interface IPmfHubClient
{
    Task InventoryStatusChangedAsync(InventoryStatusChangedMessage msg, CancellationToken ct = default);
    Task OrderCreatedAsync(OrderCreatedMessage msg, CancellationToken ct = default);
    Task OrderStatusChangedAsync(OrderStatusChangedMessage msg, CancellationToken ct = default);
    Task PrescriptionVerifiedAsync(PrescriptionVerifiedMessage msg, CancellationToken ct = default);
}

public record InventoryStatusChangedMessage(
    int InventoryId, int PharmacyId, int MedicineId,
    string Status, decimal Price, DateTime UpdatedAtUtc);

public record OrderCreatedMessage(
    int OrderId, int PharmacyId, int PatientUserId, 
    DateTime CreatedAtUtc);

public record OrderStatusChangedMessage(
    int OrderId, int PharmacyId, int PatientUserId, string Status);

public record PrescriptionVerifiedMessage(
    int PrescriptionId, int OrderId, int PharmacyId,
     int PatientUserId, bool Approved, string? Reason);


public interface IPharmacyMembershipService
{
    Task<IReadOnlyList<int>> GetPharmacyIdsForUserAsync(string userId, CancellationToken ct = default);
}