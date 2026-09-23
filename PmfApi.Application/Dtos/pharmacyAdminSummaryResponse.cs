namespace PmfApi.Application.Dtos;

public record PharmacyAdminSummaryResponse
(
    int Id,
    string Name,
    string LicenseNumber,
    bool IsVerified,
    bool IsActive,
    decimal ReliablityScore,
    string FreshnessStatus,
    int ActiveStaffCount,
    int PendingDocumentCount,
    DateTime LastInventoryUpdatedAt,
    DateTime RegisteredAt
);