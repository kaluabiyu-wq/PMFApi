
using System.Data;
using Microsoft.AspNetCore.SignalR;

namespace PmfApi.Dto;

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