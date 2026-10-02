using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Interfaces;

public interface IPrescriptionService
{
   
    Task<ServiceResult<PrescriptionResponse>> CreateAsync(int orderId, int userId, PrescriptionRequest request, CancellationToken ct);

    Task<PrescriptionResponse?> GetByIdAsync(int id, CancellationToken ct);

   
    Task<Prescription?> GetEntityByIdAsync(int id, CancellationToken ct);

    Task<List<PrescriptionResponse>> GetByOrderAsync(int orderId, CancellationToken ct);

    Task<PagedResponse<PrescriptionResponse>> GetByPharmacyAsync(int pharmacyId, PrescriptionListQuery query, CancellationToken ct);

    Task<ServiceResult<PrescriptionResponse>> ReviewAsync(int id, int reviewerUserId, PrescriptionReviewRequest request, CancellationToken ct);
}