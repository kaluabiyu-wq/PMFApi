

using Microsoft.EntityFrameworkCore;
using PmfApi.Data;
using PmfApi.Dto;
using PmfApi.Entities;

namespace PmfApi.Service;
public class MedicinesService(PmfDbContext context, ILogger<MedicinesService> logger) 
: IMedicinesService

{
  public Task<MedicineResponse?> GetByIdAsync(int id,CancellationToken ct) =>
  context.Medicines.AsNoTracking()
  .Where(m => m.Id == id)
  .Select(m => new MedicineResponse (
    m.Id,m.GenericName,m.BrandName,m.Category,
    m.DosageForm,m.Strength,m.IsActive,m.RequeiresPrescription
  )).FirstOrDefaultAsync(ct);

  public async Task<MedicineResponse> CreateAsync(MedicineRequest request,CancellationToken ct)
    {
        var medicine = new Medicine
        {
            GenericName = request.GenericName,
            BrandName = request.BrandName,
            IsActive = request.IsActice,
            RequeiresPrescription = request.RequeiresPresciption
        };
        context.Medicines.Add(medicine);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Created Medicinies {MedicineId} with {GenericName} and {BrandName} ",
         medicine.Id,medicine.GenericName,medicine.BrandName
        );
        return (await GetByIdAsync(medicine.Id,ct))!;
    }



}
