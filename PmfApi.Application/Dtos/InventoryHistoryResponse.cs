
namespace PmfApi.Application.Dtos;


public record InventoryHistoryResponse
(
    int Id,
    int InventoryId,
    int MedicineId,
    int PharmacyId,
    int UserId,
    decimal OldPrice,
    DateTime ChangedAt
);