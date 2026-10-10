namespace PmfApi.Application.Dtos;

public sealed record PharmacyDashboardResponse(
    int PharmacyId,
    string Name,
    string VerificationStatus,   // "approved" | "pending" | "rejected"
    decimal ReliabilityScore,
    int FreshnessThreshold,
    int ItemsListed,
    int OutOfStockCount,
    int FeedbackCount,
    int UnavailableReports,
    DateTime LastInventoryUpdateAt);