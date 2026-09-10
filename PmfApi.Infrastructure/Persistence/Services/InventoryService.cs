using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PmfApi.Infrastructure.Persistence.Services;

public class InventoryService(PmfDbContext context, ILogger<InventoryService> logger) : IInventoryService
{
    public Task<InventoryResponse?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Inventories
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new InventoryResponse(e.Id, e.MedicineId, e.PharmacyId, e.UpdatebyUserId, e.Price, e.Status, e.LastUpdatedAt))
            .FirstOrDefaultAsync(ct);

    public Task<InventoryResponse?> GetByPharmacyAndMedicineAsync(int pharmacyId, int medicineId, CancellationToken ct) =>
        context.Inventories
            .AsNoTracking()
            .Where(e => e.PharmacyId == pharmacyId && e.MedicineId == medicineId)
            .Select(e => new InventoryResponse(e.Id, e.MedicineId, e.PharmacyId, e.UpdatebyUserId, e.Price, e.Status, e.LastUpdatedAt))
            .FirstOrDefaultAsync(ct);

    public async Task<InventoryResponse> CreateAsync(int pharmacyId, InventoryRequest request, CancellationToken ct)
    {
        var inventory = new Inventory
        {
            PharmacyId = pharmacyId,
            MedicineId = request.MedicineId,
            Price = request.Price,
            Status = request.Status,
            UpdatebyUserId = request.UpdatebyUserId,
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
                ? query.OrderByDescending(c => c.UpdatebyUserId)
                : query.OrderBy(c => c.UpdatebyUserId)
        };

        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new InventoryResponse(c.Id, c.MedicineId, c.PharmacyId, c.UpdatebyUserId, c.Price, c.Status, c.LastUpdatedAt))
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

    public async Task<MedicinePharmacyInventoryResponse?> GetPharmaciesByMedicineAsync(
        int medicineId,
        CancellationToken ct)
    {
        var inventory = await context.Inventories
            .AsNoTracking()
            .Where(i => i.MedicineId == medicineId)
            .OrderBy(i => i.PharmacyId)
            .Select(i => new
            {
                i.MedicineId,
                i.Medicine.GenericName,
                i.Medicine.BrandName,
                i.Medicine.Category,
                i.Medicine.DosageForm,
                i.Medicine.Strength,
                i.Medicine.RequeiresPrescription,
                i.PharmacyId,
                i.Pharmacy.Name,
                i.Price,
                i.Status,
                i.LastUpdatedAt
            })
            .ToListAsync(ct);

        if (inventory.Count == 0)
            return null;

        var medicine = inventory.First();

        return new MedicinePharmacyInventoryResponse(
            medicine.MedicineId,
            medicine.GenericName,
            medicine.BrandName,
            medicine.Category,
            medicine.DosageForm,
            medicine.Strength,
            medicine.RequeiresPrescription,
            inventory.Select(i => new PharmacyInventoryDetail(
                i.PharmacyId,
                i.Name,
                i.Price,
                i.Status,
                i.LastUpdatedAt
            )).ToList()
        );
    }

    public async Task<List<MedicinePharmacyInventoryResponse>>
        GetAllMedicinesWithPharmaciesAsync(CancellationToken ct)
    {
        var inventory = await context.Inventories
            .AsNoTracking()
            .OrderBy(i => i.Medicine.GenericName)
            .ThenBy(i => i.PharmacyId)
            .Select(i => new
            {
                MedicineId = i.Medicine.Id,
                GenericName = i.Medicine.GenericName,
                BrandName = i.Medicine.BrandName,
                Category = i.Medicine.Category,
                DosageForm = i.Medicine.DosageForm,
                Strength = i.Medicine.Strength,

                RequiresPrescription = i.Medicine.RequeiresPrescription,

                PharmacyId = i.PharmacyId,
                Name = i.Pharmacy.Name,
                Price = i.Price,
                Status = i.Status,
                LastUpdatedAt = i.LastUpdatedAt
            })
            .ToListAsync(ct);

        var result = inventory
            .GroupBy(i => new
            {
                i.MedicineId,
                i.GenericName,
                i.BrandName,
                i.Category,
                i.DosageForm,
                i.Strength,
                i.RequiresPrescription
            })
            .Select(group => new MedicinePharmacyInventoryResponse(
                group.Key.MedicineId,
                group.Key.GenericName,
                group.Key.BrandName,
                group.Key.Category,
                group.Key.DosageForm,
                group.Key.Strength,
                group.Key.RequiresPrescription,
                group.Select(i => new PharmacyInventoryDetail(
                    i.PharmacyId,
                    i.Name,
                    i.Price,
                    i.Status,
                    i.LastUpdatedAt
                )).ToList()
            ))
            .ToList();

        return result;
    }
}