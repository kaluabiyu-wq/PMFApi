using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

public class PrescriptionService(PmfDbContext context, ILogger<PrescriptionService> logger) : IPrescriptionService
{
    public async Task<ServiceResult<PrescriptionResponse>> CreateAsync(int orderId, int userId,
     PrescriptionRequest request, CancellationToken ct)
    {
        
        if (!Uri.TryCreate(request.FileUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return ServiceResult<PrescriptionResponse>.Invalid("FileUrl must be an absolute http or https URL.");

        await using var tx = await context.Database.BeginTransactionAsync(ct);

        
        var locked = await context.Orders
            .Where(o => o.Id == orderId && o.UserId == userId && o.Status == OrderStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.UpdatedAt, DateTime.UtcNow), ct);

        if (locked == 0)
        {
           
            return await context.Orders.AnyAsync(o => o.Id == orderId && o.UserId == userId, ct)
                ? ServiceResult<PrescriptionResponse>.Conflict("A prescription can only be submitted while the order is Pending.")
                : ServiceResult<PrescriptionResponse>.NotFound($"No order exists with id {orderId}.");
        }

        var requiresRx = await context.OrderItems
            .AnyAsync(i => i.OrderId == orderId && i.Inventory.Medicine.RequeiresPrescription, ct);

        if (!requiresRx)
            return ServiceResult<PrescriptionResponse>.Invalid("This order contains no prescription-only medicines, so no prescription is needed.");

        if (await context.Prescriptions.AnyAsync(p => p.OrderId == orderId && p.VerificationStatus == VerificationStatus.Approved, ct))
            return ServiceResult<PrescriptionResponse>.Conflict("A prescription has already been approved for this order.");

        if (await context.Prescriptions.AnyAsync(p => p.OrderId == orderId && p.VerificationStatus == VerificationStatus.Pending, ct))
            return ServiceResult<PrescriptionResponse>.Conflict("A prescription is already awaiting review for this order.");

        var prescription = new Prescription
        {
            OrderId = orderId,
            FileUrl = request.FileUrl,
            SubmittedAt = DateTime.UtcNow,
            VerificationStatus = VerificationStatus.Pending,
        };

        context.Prescriptions.Add(prescription);

        try
        {
            await context.SaveChangesAsync(ct);
            var pharmacyId = await context.Orders.AsNoTracking()
                .Where(o => o.Id == orderId).Select(o => o.PharmacyId).FirstAsync(ct);

            await AlertWriter.ForPharmacyStaffAsync(
                context, pharmacyId, AlertEventType.PrescriptionSubmitted,
                AlertReferenceTables.Prescriptions, prescription.Id,
                $"A prescription for order #{orderId} was submitted and is waiting for review.", ct);

            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex) when (PostgresErrors.IsUniqueViolation(ex))
        {
            
            return ServiceResult<PrescriptionResponse>.Conflict("A prescription is already awaiting review for this order.");
        }

        logger.LogInformation("User {UserId} submitted Prescription {PrescriptionId} for Order {OrderId}", userId, prescription.Id, orderId);

        return ServiceResult<PrescriptionResponse>.Ok((await GetByIdAsync(prescription.Id, ct))!);
    }

    public Task<PrescriptionResponse?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Prescriptions.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PrescriptionResponse(
                p.Id, p.OrderId, p.FileUrl, p.SubmittedAt, p.VerificationStatus,
                p.VerifiedByUserId, p.VerifiedAt, p.ReviewNote))
            .FirstOrDefaultAsync(ct);

    public Task<Prescription?> GetEntityByIdAsync(int id, CancellationToken ct) =>
        context.Prescriptions.AsNoTracking()
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<List<PrescriptionResponse>> GetByOrderAsync(int orderId, CancellationToken ct) =>
        context.Prescriptions.AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.Id)
            .Select(p => new PrescriptionResponse(
                p.Id, p.OrderId, p.FileUrl, p.SubmittedAt, p.VerificationStatus,
                p.VerifiedByUserId, p.VerifiedAt, p.ReviewNote))
            .ToListAsync(ct);

    public async Task<PagedResponse<PrescriptionResponse>> GetByPharmacyAsync(int pharmacyId, PrescriptionListQuery query, CancellationToken ct)
    {
        
        var source = context.Prescriptions.AsNoTracking().Where(p => p.Order.PharmacyId == pharmacyId);

        if (query.Status is { } status)
            source = source.Where(p => p.VerificationStatus == status);

        var page = Math.Max(1, query.Page);
        var total = await source.CountAsync(ct);

       
        var ordered = query.Status == VerificationStatus.Pending
            ? source.OrderBy(p => p.SubmittedAt).ThenBy(p => p.Id)
            : source.OrderByDescending(p => p.SubmittedAt).ThenByDescending(p => p.Id);

        var items = await ordered
            .Skip((page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new PrescriptionResponse(
                p.Id, p.OrderId, p.FileUrl, p.SubmittedAt, p.VerificationStatus,
                p.VerifiedByUserId, p.VerifiedAt, p.ReviewNote))
            .ToListAsync(ct);

        return new PagedResponse<PrescriptionResponse>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = query.PageSize,
        };
    }

    public async Task<ServiceResult<PrescriptionResponse>> ReviewAsync(int id, int reviewerUserId,
    
     PrescriptionReviewRequest request, CancellationToken ct)
    {
        var decision = request.VerificationStatus;

        if (decision is not (VerificationStatus.Approved or VerificationStatus.Rejected))
            return ServiceResult<PrescriptionResponse>.Invalid("VerificationStatus must be Approved or Rejected.");

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        if (decision == VerificationStatus.Rejected && note is null)
            return ServiceResult<PrescriptionResponse>.Invalid("A note explaining the rejection is required.");

        var orderId = await context.Prescriptions.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => (int?)p.OrderId)
            .FirstOrDefaultAsync(ct);

        if (orderId is null)
            return ServiceResult<PrescriptionResponse>.NotFound($"No prescription exists with id {id}.");

        await using var tx = await context.Database.BeginTransactionAsync(ct);

        
        var locked = await context.Orders
            .Where(o => o.Id == orderId && o.Status == OrderStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.UpdatedAt, DateTime.UtcNow), ct);

        if (locked == 0)
            return ServiceResult<PrescriptionResponse>.Conflict("The order is no longer Pending, so its prescription can't be reviewed.");

        var now = DateTime.UtcNow;

       
        var rows = await context.Prescriptions
            .Where(p => p.Id == id && p.VerificationStatus == VerificationStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.VerificationStatus, decision)
                .SetProperty(p => p.VerifiedByUserId, (int?)reviewerUserId)
                .SetProperty(p => p.VerifiedAt, (DateTime?)now)
                .SetProperty(p => p.ReviewNote, note), ct);

        if (rows == 0)
            return ServiceResult<PrescriptionResponse>.Conflict("This prescription has already been reviewed.");

        var patientUserId = await context.Orders.AsNoTracking()
            .Where(o => o.Id == orderId).Select(o => o.UserId).FirstAsync(ct);

        AlertWriter.ForUser(
            context, patientUserId,
            decision == VerificationStatus.Approved ? AlertEventType.PrescriptionApproved : AlertEventType.PrescriptionRejected,
            AlertReferenceTables.Prescriptions, id,
            decision == VerificationStatus.Approved
                ? $"Your prescription for order #{orderId} was approved."
                : $"Your prescription for order #{orderId} was rejected. Open it to see the reason.");

        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogInformation("Prescription {PrescriptionId} for Order {OrderId} {Decision} by User {ReviewerId}",
            id, orderId, decision, reviewerUserId);

        return ServiceResult<PrescriptionResponse>.Ok((await GetByIdAsync(id, ct))!);
    }
}