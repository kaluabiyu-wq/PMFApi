namespace PmfApi.Application.Dtos;


public record PharmacyAdminProfileResponse
(
    int Id,
    string Name,
    string LicenseNumber,
    int PhoneNumber,
    string? Email,
    bool IsVerified,
    bool IsActive,
    decimal ReliablityScore,
    string FreshnessStatus,
    int FreshnessThreshold,
    DateTime LastInventoryUpdatedAt,
    DateTime RegisteredAt,
    int LocationId,
    int TotalStaffCount,
    int ActiveStaffCount,
    int InventoryItemCount,
    int PendingDocumentCount,
    int ApprovedDocumentCount,
    int RejectedDocumentCount,
    int ReviewCount,
    double? AverageRating
);