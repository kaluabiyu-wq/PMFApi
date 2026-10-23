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


  public async Task<PagedResponse<InventoryPriceChangeResponse>> GetPriceChangesAsync(
    int inventoryId, PagedRequest request, CancellationToken ct)
  {
    var page = Math.Max(request.Page, 1);

    var baseQuery = context.InventoryHistories.AsNoTracking()
      .Where(h => h.InventoryId == inventoryId);

    var total = await baseQuery.CountAsync(ct);

    // Newest first by default. Id is the tie-breaker so two changes in the same tick keep a stable order.
    var ordered = request.Descending || request.OrderBy == "Title"
      ? baseQuery.OrderByDescending(h => h.ChangedAt).ThenByDescending(h => h.Id)
      : baseQuery.OrderBy(h => h.ChangedAt).ThenBy(h => h.Id);

    var items = await ordered
      .Skip((page - 1) * request.PageSize)
      .Take(request.PageSize)
      .Select(h => new InventoryPriceChangeResponse(
        h.Id,
        h.InventoryId,
        h.MedicineId,
        h.PharmacyId,
        h.UserId,
        h.User.FullName,
        h.OldPrice,
        // The price this change produced = the next change's OldPrice, else the live price.
        context.InventoryHistories
          .Where(n => n.InventoryId == h.InventoryId
                      && (n.ChangedAt > h.ChangedAt || (n.ChangedAt == h.ChangedAt && n.Id > h.Id)))
          .OrderBy(n => n.ChangedAt).ThenBy(n => n.Id)
          .Select(n => (decimal?)n.OldPrice)
          .FirstOrDefault() ?? h.Inventory.Price,
        h.ChangedAt))
      .ToListAsync(ct);

    return new PagedResponse<InventoryPriceChangeResponse>
    {
      Items = items,
      TotalCount = total,
      Page = page,
      PageSize = request.PageSize,
    };
  }
}