using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PmfApi.Infrastructure.Persistence.Services;
public class InventoryHistoryService(PmfDbContext context,ILogger <InventoryHistoryService> logger)
:IInventoryHistoryService
{
  public async  Task<InventoryHistoryResponse> CreateAsync(int InventoryId,InventoryHistoryRequest request,CancellationToken ct)
    {
        var history = new InventoryHistory
        {
          InventoryId = InventoryId,
          MedicineId = request.MedicineId,
          PharmacyId = request.PharmacyId,
          UserId = request.UserId,
          OldPrice = request.OldPrice,
          ChangedAt = DateTime.UtcNow

        };
        context.InventoryHistories.Add(history);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created Inventory History {InventoryHistoryId}  {InventoryId} {MedicineId} {PharamacyId} {UserId} {OldPrice}",
            history.Id,history.InventoryId,history.MedicineId,history.PharmacyId,history.UserId,history.OldPrice);

        return (await GetByinventoryIdAsync(history.InventoryId,history.Id,ct))!;

        
    }
  public Task<InventoryHistoryResponse?> GetByinventoryIdAsync(int InventoryId,int id,CancellationToken ct) =>
  context.InventoryHistories.AsNoTracking()
  .Where(h => h.Id == id && h.InventoryId == InventoryId)
  .Select(h => new InventoryHistoryResponse(
    h.Id,h.InventoryId, h.MedicineId,
    h.PharmacyId,h.UserId,h.OldPrice,h.ChangedAt
  )).FirstOrDefaultAsync(ct);

}
