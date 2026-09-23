using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IReviewService
{
    Task<ReviewResponse> CreateAsync(int pharmacyId, ReviewRequest request, CancellationToken ct);

    Task<ReviewResponse?> GetByPharmacyIdAsync(int pharmacyId, int id, CancellationToken ct);

    Task<PagedResponse<ReviewResponse>> GetByPharmacyAsync(int pharmacyId, PagedRequest request, CancellationToken ct);

    // Service-experience rating only — never factored into Pharmacy.ReliablityScore,
    // which is driven purely by inventory freshness/accuracy.
    Task<double?> GetAverageRatingAsync(int pharmacyId, CancellationToken ct);
}
