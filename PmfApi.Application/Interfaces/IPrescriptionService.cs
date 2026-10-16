using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;

namespace PmfApi.Application.Interfaces;

public interface IPrescriptionService
{
   
    Task<ServiceResult<PrescriptionResponse>> CreateAsync(int orderId, int userId, PrescriptionRequest request, CancellationToken ct);

    /// <summary>Same rules as <see cref="CreateAsync"/>, but the prescription is a file this API
    /// already stored; <paramref name="storedPath"/> is the relative path returned by IFileStorage.</summary>
    Task<ServiceResult<PrescriptionResponse>> CreateFromUploadAsync(int orderId, int userId, string storedPath, CancellationToken ct);

    Task<PrescriptionResponse?> GetByIdAsync(int id, CancellationToken ct);

   
    Task<Prescription?> GetEntityByIdAsync(int id, CancellationToken ct);

    Task<List<PrescriptionResponse>> GetByOrderAsync(int orderId, CancellationToken ct);

    Task<PagedResponse<PrescriptionResponse>> GetByPharmacyAsync(int pharmacyId, PrescriptionListQuery query, CancellationToken ct);

    Task<ServiceResult<PrescriptionResponse>> ReviewAsync(int id, int reviewerUserId, PrescriptionReviewRequest request, CancellationToken ct);
}