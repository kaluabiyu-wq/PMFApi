using PmfApi.Dto;
using PmfApi.Entities;

public interface IMedicinesService
{
   Task<MedicineResponse> CreateAsync(MedicineRequest request,CancellationToken ct);

  Task<MedicineResponse?> GetByIdAsync(int id,CancellationToken ct);

  
}