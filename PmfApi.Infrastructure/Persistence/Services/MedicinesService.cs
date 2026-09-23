

using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PmfApi.Infrastructure.Persistence.Services;
public class MedicinesService(PmfDbContext context, ILogger<MedicinesService> logger) 
: IMedicinesService

{
  public Task<MedicineResponse?> GetByIdAsync(int id,CancellationToken ct) =>
  context.Medicines.AsNoTracking()
  .Where(m => m.Id == id)
  .Select(m => new MedicineResponse (
    m.Id,m.GenericName,m.BrandName,
    m.Category,m.DosageForm,m.Strength,
    m.RequeiresPrescription,m.IsActive
  )).FirstOrDefaultAsync(ct);

  public async Task<MedicineResponse> CreateAsync(MedicineRequest request,CancellationToken ct)
    {
        var medicine = new Medicine
        {
            GenericName = request.GenericName,
            BrandName = request.BrandName,
            RequeiresPrescription = request.RequeiresPresciption
        };
        context.Medicines.Add(medicine);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Created Medicinies {MedicineId} with {GenericName}",
         medicine.Id,medicine.GenericName);
        return (await GetByIdAsync(medicine.Id,ct))!;
    }

public async Task<PagedResponse<MedicineResponse>> GetMedicineAsync(PagedRequest request, CancellationToken ct)
{
    IQueryable<Medicine> query = context.Medicines.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        query = query.Where(c => EF.Functions.ILike(c.GenericName, "$%{request.Search}%")
                               || EF.Functions.ILike(c.BrandName, $"%{request.Search}%"));
    }

    var totalCount = await query.CountAsync(ct);

    IOrderedQueryable<Medicine> sortedQuery = request.OrderBy switch
    {
        "GenericName" => request.Descending
            ? query.OrderByDescending(c => c.GenericName)
            : query.OrderBy(c => c.GenericName),
        "BrandName" => request.Descending
            ? query.OrderByDescending(c => c.BrandName)
            : query.OrderBy(c => c.BrandName),
        "Category" => request.Descending
            ? query.OrderByDescending(c => c.Category)
            : query.OrderBy(c => c.Category),
        "DosageForm" => request.Descending
            ? query.OrderByDescending(c => c.DosageForm)
            : query.OrderBy(c => c.DosageForm),
        _ =>  request.Descending
            ? query.OrderByDescending(c => c.RequeiresPrescription)
            : query.OrderBy(c => c.RequeiresPrescription)
    };

    var items = await sortedQuery
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(m => new MedicineResponse(m.Id, m.GenericName,m.BrandName,
        m.Category,m.DosageForm,m.Strength,m.RequeiresPrescription,
        m.IsActive))
        .ToListAsync(ct);

    return new PagedResponse<MedicineResponse>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}
}
