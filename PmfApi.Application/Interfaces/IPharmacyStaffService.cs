using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IPharmacyStaffService
{
    Task<PharmacyStaffResponse> CreateAsync(int pharmacyId, PharmacyStaffRequest request, CancellationToken ct);

    Task<PharmacyStaffResponse?> GetByPharmacyIdAsync(int pharmacyId, int id, CancellationToken ct);

    Task<List<PharmacyStaffResponse>> GetByPharmacyAsync(int pharmacyId, CancellationToken ct);

    Task<bool> IsAuthorizedForPharmacyAsync(int userId, int pharmacyId, CancellationToken ct);

    Task<PharmacyStaffResponse?> DeactivateAsync(int pharmacyId, int id, CancellationToken ct);
}
