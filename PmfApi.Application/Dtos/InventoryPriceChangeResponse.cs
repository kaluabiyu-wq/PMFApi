namespace PmfApi.Application.Dtos;
public record InventoryPriceChangeResponse(
    int Id,
    int InventoryId,
    int MedicineId,
    int PharmacyId,
    int ChangedByUserId,
    string? ChangedByName,
    decimal OldPrice,
    decimal NewPrice,
    DateTime ChangedAt
);