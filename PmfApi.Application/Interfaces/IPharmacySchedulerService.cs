using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IPharmaciesScheduleService
{
    Task<PharmacyScheduleResponse> CreateAsync(int pharmacyId, PharmaciesScheduleRequest request, CancellationToken ct);

    Task<PharmacyScheduleResponse?> GetByPhramacyIdAsync(int pharmacyId, int id, CancellationToken ct);

    Task<IReadOnlyList<PharmacyScheduleResponse>> GetWeekAsync(int pharmacyId, CancellationToken ct);

    Task<IReadOnlyList<PharmacyScheduleResponse>> SaveWeekAsync(
        int pharmacyId, IReadOnlyList<PharmaciesScheduleRequest> days, CancellationToken ct);
}