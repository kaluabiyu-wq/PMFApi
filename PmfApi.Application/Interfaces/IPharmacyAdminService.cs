using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IPharmacyAdminService
{
    Task<PagedResponse<PharmacyAdminSummaryResponse>> GetAllAsync(PharmacyAdminQuery query, CancellationToken ct);

    Task<PharmacyAdminProfileResponse?> GetProfileAsync(int pharmacyId, CancellationToken ct);

    Task<PharmacyResponse?> UpdateAsync(int pharmacyId, PharmacyUpdateRequest request, CancellationToken ct);

    Task<PharmacyResponse?> SetStatusAsync(int pharmacyId, PharmacyStatusUpdateRequest request, CancellationToken ct);

    Task<bool> DeleteAsync(int pharmacyId, CancellationToken ct);
}