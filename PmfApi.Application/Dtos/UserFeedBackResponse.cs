
using System.Security.Cryptography.X509Certificates;

namespace PmfApi.Application.Dtos;


public record UserFeedBackResponse
(
  int Id,
  int UserId,
  int InventoryId,
  int PharmacyId,
  bool WasMedicineAvailable,
  string? Comments,
  DateTime SubmittedAt
);