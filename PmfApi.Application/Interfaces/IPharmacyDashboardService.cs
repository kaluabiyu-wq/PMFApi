using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IPharmacyDashboardService
{
    Task<PharmacyDashboardResponse?> GetAsync(int pharmacyId, CancellationToken ct);
}