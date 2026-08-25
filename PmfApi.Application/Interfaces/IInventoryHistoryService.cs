
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;


namespace PmfApi.Application.Interfaces;
public interface IInventoryHistoryService
{
Task<InventoryHistoryResponse> CreateAsync(int InventoryId,InventoryHistoryRequest request,CancellationToken ct);
Task<InventoryHistoryResponse?> GetByinventoryIdAsync(int InventoryId,int id,CancellationToken ct);

 
}





