using Microsoft.EntityFrameworkCore;
using PmfApi.Data;
using PmfApi.Dto;
using PmfApi.Entities;
using PmfApi.Interface;

namespace PmfApi.Service;

public class InventoryService(PmfDbContext context, ILogger<InventoryService> logger) : IInventoryService
{
    public Task<InventoryResponse?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Inventories
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new InventoryResponse(e.Id, e.MedicineId, e.PharmacyId, e.UserId, e.Price, e.Status, e.LastUpdatedAt))
            .FirstOrDefaultAsync(ct);

    public Task<InventoryResponse?> GetByPharmacyAndMedicineAsync(int pharmacyId, int medicineId, CancellationToken ct) =>
        context.Inventories
            .AsNoTracking()
            .Where(e => e.PharmacyId == pharmacyId && e.MedicineId == medicineId)
            .Select(e => new InventoryResponse(e.Id, e.MedicineId, e.PharmacyId, e.UserId, e.Price, e.Status, e.LastUpdatedAt))
            .FirstOrDefaultAsync(ct);

    public async Task<InventoryResponse> CreateAsync(int pharmacyId, InventoryRequest request, CancellationToken ct)
    {
        var inventory = new Inventory
        {
            PharmacyId = pharmacyId,
            MedicineId = request.MedicineId,
            Price = request.Price,
            Status = request.Status,
            UserId = request.UserId,
            LastUpdatedAt = DateTime.UtcNow
        };

        context.Inventories.Add(inventory);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Inventory {InventoryId} created for Medicine {MedicineId} in Pharmacy {PharmacyId} at Price {Price}",
            inventory.Id, inventory.MedicineId, inventory.PharmacyId, inventory.Price);

        return (await GetByIdAsync(inventory.Id, ct))!;
    }

    public async Task<PagedResponse<InventoryResponse>> GetInventoryAsync(PagedRequest request, CancellationToken ct)
    {
        IQueryable<Inventory> query = context.Inventories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(c => EF.Functions.ILike(c.Status, $"%{request.Search}%"));
        }

        var totalCount = await query.CountAsync(ct);

        IOrderedQueryable<Inventory> sortedQuery = request.OrderBy switch
        {
            "PharmacyId" => request.Descending
                ? query.OrderByDescending(c => c.PharmacyId)
                : query.OrderBy(c => c.PharmacyId),
            "MedicineId" => request.Descending
                ? query.OrderByDescending(c => c.MedicineId)
                : query.OrderBy(c => c.MedicineId),
            "Price" => request.Descending
                ? query.OrderByDescending(c => c.Price)
                : query.OrderBy(c => c.Price),
            _ => request.Descending
                ? query.OrderByDescending(c => c.UserId)
                : query.OrderBy(c => c.UserId)
        };

        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new InventoryResponse(c.Id, c.MedicineId, c.PharmacyId, c.UserId, c.Price, c.Status, c.LastUpdatedAt))
            .ToListAsync(ct);

        return new PagedResponse<InventoryResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public Task<List<PharmacyMedicineDetail>> GetMedicineDetailsByPharmacyAsync(int pharmacyId, CancellationToken ct) =>
        context.Inventories
            .AsNoTracking()
            .Where(i => i.PharmacyId == pharmacyId)
            .OrderBy(i => i.Medicine.GenericName)
            .Select(i => new PharmacyMedicineDetail(
                i.Medicine.Id,
                i.Medicine.GenericName,
                i.Medicine.BrandName,
                i.Medicine.Category,
                i.Medicine.DosageForm,
                i.Medicine.Strength,
                i.Medicine.RequeiresPrescription,
                i.Price,
                i.Status,
                i.LastUpdatedAt))
            .ToListAsync(ct);
}