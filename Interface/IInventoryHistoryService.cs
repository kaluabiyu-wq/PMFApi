
using PmfApi.Dto;
using PmfApi.Entities;

public interface IInventoryHistoryService
{
Task<InventoryHistoryResponse> CreateAsync(int InventoryId,InventoryHistoryRequest request,CancellationToken ct);
Task<InventoryHistoryResponse?> GetByinventoryIdAsync(int InventoryId,int id,CancellationToken ct);
}





