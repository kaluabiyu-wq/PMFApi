
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;


namespace PmfApi.Application.Interfaces;
public interface IMedicinesService
{
   Task<MedicineResponse> CreateAsync(MedicineRequest request,CancellationToken ct);

  Task<MedicineResponse?> GetByIdAsync(int id,CancellationToken ct);

    Task<PagedResponse<MedicineResponse>> GetMedicineAsync(PagedRequest request, CancellationToken ct);
  
}