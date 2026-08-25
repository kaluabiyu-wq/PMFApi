

namespace PmfApi.Dto;

public record InventoryResponse(
   int Id,
   int MedicineId,
   int PharmacyId,
   int UpdateUserId,
   decimal Price,
   string Status,
   DateTime LastUpdatedAt
);