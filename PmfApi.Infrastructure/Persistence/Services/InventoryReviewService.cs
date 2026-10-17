using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PmfApi.Application.Abstractions;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

public class InventoryReviewService(
    PmfDbContext context,
    ILogger<InventoryReviewService> logger,
    IPmfHubClient hub) : IInventoryReviewService
{
   public Task<List<InventorySubmissionResponse>> GetPharmacySubmissionsAsync(int pharmacyId, CancellationToken ct) =>
    Project(context.Inventories
            .AsNoTracking()
            .Where(i => i.PharmacyId == pharmacyId && i.ApprovalStatus != InventoryApprovalStatus.Approved)
            .OrderByDescending(i => i.LastUpdatedAt))
        .ToListAsync(ct);

public Task<List<InventorySubmissionResponse>> GetSubmissionsAsync(string? approvalStatus, CancellationToken ct)
{
    IQueryable<Inventory> query = context.Inventories.AsNoTracking();

    if (InventoryApprovalStatus.IsValid(approvalStatus))
        query = query.Where(i => i.ApprovalStatus == approvalStatus);

   
    query = approvalStatus == InventoryApprovalStatus.Pending
        ? query.OrderBy(i => i.LastUpdatedAt)
        : query.OrderByDescending(i => i.LastUpdatedAt);

    return Project(query).ToListAsync(ct);
}

    public async Task<ReviewResult> ApproveAsync(int inventoryId, int reviewerUserId, CancellationToken ct)
    {
        var inventory = await context.Inventories.FirstOrDefaultAsync(i => i.Id == inventoryId, ct);
        if (inventory is null) return new(ReviewOutcome.NotFound, null);
        if (inventory.ApprovalStatus != InventoryApprovalStatus.Pending) return new(ReviewOutcome.NotPending, null);

        var now = DateTime.UtcNow;
        inventory.ApprovalStatus = InventoryApprovalStatus.Approved;
        inventory.RejectionReason = null;
        inventory.ReviewedByUserId = reviewerUserId;
        inventory.ReviewedAt = now;
        inventory.LastUpdatedAt = now;

        var pharmacy = await context.Pharmacies.FirstOrDefaultAsync(p => p.Id == inventory.PharmacyId, ct);
        if (pharmacy is not null && now > pharmacy.LastInventoryUpdateAt)
            pharmacy.LastInventoryUpdateAt = now;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Inventory {InventoryId} approved by user {UserId}", inventory.Id, reviewerUserId);

        await hub.InventoryStatusChangedAsync(
            new(inventory.Id, inventory.PharmacyId, inventory.MedicineId,
                inventory.Status, inventory.Price, inventory.LastUpdatedAt), ct);

        return new(ReviewOutcome.Done, await GetOneAsync(inventory.Id, ct));
    }

    public async Task<ReviewResult> RejectAsync(int inventoryId, int reviewerUserId, string reason, CancellationToken ct)
    {
        var inventory = await context.Inventories.FirstOrDefaultAsync(i => i.Id == inventoryId, ct);
        if (inventory is null) return new(ReviewOutcome.NotFound, null);
        if (inventory.ApprovalStatus != InventoryApprovalStatus.Pending) return new(ReviewOutcome.NotPending, null);

        var now = DateTime.UtcNow;
        inventory.ApprovalStatus = InventoryApprovalStatus.Rejected;
        inventory.RejectionReason = reason.Trim();
        inventory.ReviewedByUserId = reviewerUserId;
        inventory.ReviewedAt = now;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Inventory {InventoryId} rejected by user {UserId}", inventory.Id, reviewerUserId);

        return new(ReviewOutcome.Done, await GetOneAsync(inventory.Id, ct));
    }

    private Task<InventorySubmissionResponse?> GetOneAsync(int id, CancellationToken ct) =>
        Project(context.Inventories.AsNoTracking().Where(i => i.Id == id))
            .FirstOrDefaultAsync(ct);

    private static IQueryable<InventorySubmissionResponse> Project(IQueryable<Inventory> query) =>
        query.Select(i => new InventorySubmissionResponse(
            i.Id,
            i.PharmacyId,
            i.Pharmacy.Name,
            i.MedicineId,
            i.Medicine.GenericName,
            i.Medicine.BrandName,
            i.Medicine.Category,
            i.Medicine.DosageForm,
            i.Medicine.Strength,
            i.Medicine.RequeiresPrescription,
            i.Price,
            i.ApprovalStatus,
            i.RejectionReason,
            i.UpdatebyUserId,
            i.LastUpdatedAt,
            i.ReviewedAt));
}
