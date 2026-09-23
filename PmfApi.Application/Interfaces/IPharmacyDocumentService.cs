using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IPharmacyDocumentService
{
    Task<PharmacyDocumentResponse> CreateAsync(int pharmacyId, PharmacyDocumentRequest request, CancellationToken ct);

    Task<PharmacyDocumentResponse?> GetByPharmacyIdAsync(int pharmacyId, int id, CancellationToken ct);

    Task<List<PharmacyDocumentResponse>> GetByPharmacyAsync(int pharmacyId, CancellationToken ct);

       Task<PharmacyDocumentResponse?> ReviewAsync(int pharmacyId, int id, PharmacyDocumentReviewRequest request, CancellationToken ct);
}
