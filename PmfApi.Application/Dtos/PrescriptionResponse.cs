using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;

public record PrescriptionResponse(
    int Id,
    int OrderId,
    string FileUrl,
    DateTime SubmittedAt,
    VerificationStatus VerificationStatus,
    int? VerifiedByUserId,
    DateTime? VerifiedAt,
    string? ReviewNote
);