using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Interfaces;

public interface IPharmacyDocumentService
{
    Task<PharmacyDocumentResponse> CreateAsync(int pharmacyId, PharmacyDocumentRequest request, CancellationToken ct);

    Task<PharmacyDocumentResponse> CreateFromUploadAsync(int pharmacyId, DocumentType type, string storedPath, DateTime? expiresAt, CancellationToken ct);

    Task<PharmacyDocumentResponse?> ReplaceFileAsync(int pharmacyId, int id, string storedPath, DateTime? expiresAt, CancellationToken ct);

    Task<PharmacyDocumentResponse?> GetByPharmacyIdAsync(int pharmacyId, int id, CancellationToken ct);

    Task<List<PharmacyDocumentResponse>> GetByPharmacyAsync(int pharmacyId, CancellationToken ct);

    Task<PharmacyDocumentResponse?> ReviewAsync(int pharmacyId, int id, int reviewerId, PharmacyDocumentReviewRequest request, CancellationToken ct);
}