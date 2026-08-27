using PmfApi.Domain.Entities;

namespace PmfApi.Application.Dtos;

public record PharmacyDocumentResponse(
    int Id,
    int PharmacyId,
    DocumentType DocumentType,
    string FileUrl,
    DateTime UploadedAt,
    int? ReviewedByUserId,
    ReviewStatus ReviewStatus,
    DateTime? ExpiresAt
);
