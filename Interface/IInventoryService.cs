

using PmfApi.Dto;
using PmfApi.Entities;

namespace PmfApi.Interface;
public interface IInventoryService
{

  Task<InventoryResponse> CreateAsync(int pharmacyId, int medicineId,InventoryRequest request,CancellationToken ct);

  Task<InventoryResponse?> GetByIdAsync(int pharmacyId,int medicineId, int id,CancellationToken ct);
 
    

}





