using System.ComponentModel.DataAnnotations;

namespace PmfApi.Application.Dtos;

public record InventorySubmissionResponse(
    int Id,
    int PharmacyId,
    string PharmacyName,
    int MedicineId,
    string GenericName,
    string BrandName,
    string? Category,
    string? DosageForm,
    string? Strength,
    bool RequiresPrescription,
    decimal Price,
    string ApprovalStatus,
    string? RejectionReason,
    int SubmittedByUserId,
    DateTime LastUpdatedAt,
    DateTime? ReviewedAt
);

public record RejectInventoryRequest
{
    [Required, MaxLength(500)]
    public required string Reason {get;init;}
}

public enum ReviewOutcome
{
    Done,
    NotFound,
    NotPending
}

public record ReviewResult(ReviewOutcome Outcome, InventorySubmissionResponse? Item);
