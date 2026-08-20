

using Microsoft.EntityFrameworkCore;
using PmfApi.Data;
using PmfApi.Dto;
using PmfApi.Entities;
using PmfApi.Interface;

namespace PmfApi.Service;

public class InventoryService(PmfDbContext context, ILogger<InventoryService> logger) :
IInventoryService
{
    public Task<InventoryResponse?> GetByIdAsync(int pharmacyId, int medicineId, int id,CancellationToken ct) =>
     context.Inventories
     .AsNoTracking()
     .Where(e => e.Id == id && e.MedicineId == medicineId && e.PharmacyId == pharmacyId)
     .Select(e => new InventoryResponse (e.Id, e.MedicineId, e.PharmacyId,e.UserId, e.Price,e.Status,e.LastUpdatedAt))
     .FirstOrDefaultAsync(ct);

     public async Task<InventoryResponse> CreateAsync(int pharmacyId, int medicineId,InventoryRequest request,CancellationToken ct)
    {
        var inventory = new Inventory
        {
            PharmacyId = pharmacyId,
            MedicineId = pharmacyId,
            UserId = request.UserId,
            LastUpdatedAt = DateTime.UtcNow
        };

        context.Inventories.Add(inventory);
        await context.SaveChangesAsync(ct);
        
        logger.LogInformation("Inventory {InventoryId} Created for Medicine {MedicineId} in Pharmacy {PharmacyId} with {PriceId} Price",
           inventory.Id,inventory.MedicineId,inventory.PharmacyId,inventory.Price
        );
        return (await GetByIdAsync(pharmacyId,medicineId,inventory.Id,ct))!;

        throw new NotImplementedException();
        
    }
}