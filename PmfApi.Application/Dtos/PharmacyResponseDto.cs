
using System.Data;

namespace PmfApi.Application.Dtos;


public record PharmacyResponse
(
    int Id,
    string Name,
    string LicenseNumber,
    int LocationId,
    bool IsVerified,
    decimal ReliablityScore,
    int FreshnessThreshold,
    DateTime LastInventoryUpdatedAt,
    DateTime RegisteredAt 
);