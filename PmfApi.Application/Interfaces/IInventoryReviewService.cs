using PmfApi.Application.Dtos;

namespace PmfApi.Application.Interfaces;

public interface IInventoryReviewService
{
    Task<List<InventorySubmissionResponse>> GetPharmacySubmissionsAsync(int pharmacyId, CancellationToken ct);

    Task<List<InventorySubmissionResponse>> GetSubmissionsAsync(string? approvalStatus, CancellationToken ct);

    Task<ReviewResult> ApproveAsync(int inventoryId, int reviewerUserId, CancellationToken ct);

    Task<ReviewResult> RejectAsync(int inventoryId, int reviewerUserId, string reason, CancellationToken ct);
}
