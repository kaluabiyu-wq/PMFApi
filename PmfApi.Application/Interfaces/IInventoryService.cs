
using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryResponse> CreateAsync(int pharmacyId, InventoryRequest request, CancellationToken ct);

    Task<InventoryResponse?> GetByIdAsync(int id, CancellationToken ct);

    Task<InventoryResponse?> GetByPharmacyAndMedicineAsync(int pharmacyId, int medicineId, CancellationToken ct);

    Task<PagedResponse<InventoryResponse>> GetInventoryAsync(PagedRequest request, CancellationToken ct);

    
    Task<List<PharmacyMedicineDetail>> GetMedicineDetailsByPharmacyAsync(int pharmacyId, CancellationToken ct);
}