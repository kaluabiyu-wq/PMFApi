using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PmfApi.Infrastructure.Persistence.Services;

public class PharmacyDocumentService(PmfDbContext context, ILogger<PharmacyDocumentService> logger)
: IPharmacyDocumentService
{
    public async Task<PharmacyDocumentResponse> CreateAsync(int pharmacyId, PharmacyDocumentRequest request, CancellationToken ct)
    {
        var document = new PharmacyDocument
        {
            PharmacyId = pharmacyId,
            DocumentType = request.DocumentType,
            FileUrl = request.FileUrl,
            ExpiresAt = request.ExpiresAt,
            UploadedAt = DateTime.UtcNow,
            ReviewStatus = ReviewStatus.Pending,
        };

        context.PharmacyDocuments.Add(document);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Uploaded {DocumentType} document {DocumentId} for Pharmacy {PharmacyId}",
            document.DocumentType, document.Id, document.PharmacyId);

        return (await GetByPharmacyIdAsync(document.PharmacyId, document.Id, ct))!;
    }

    public Task<PharmacyDocumentResponse?> GetByPharmacyIdAsync(int pharmacyId, int id, CancellationToken ct) =>
    context.PharmacyDocuments.AsNoTracking()
    .Where(d => d.Id == id && d.PharmacyId == pharmacyId)
    .Select(d => new PharmacyDocumentResponse(
        d.Id, d.PharmacyId, d.DocumentType, d.FileUrl, d.UploadedAt,
        d.ReviewedByUserId, d.ReviewStatus, d.ExpiresAt
    )).FirstOrDefaultAsync(ct);

    public Task<List<PharmacyDocumentResponse>> GetByPharmacyAsync(int pharmacyId, CancellationToken ct) =>
    context.PharmacyDocuments.AsNoTracking()
    .Where(d => d.PharmacyId == pharmacyId)
    .OrderByDescending(d => d.UploadedAt)
    .Select(d => new PharmacyDocumentResponse(
        d.Id, d.PharmacyId, d.DocumentType, d.FileUrl, d.UploadedAt,
        d.ReviewedByUserId, d.ReviewStatus, d.ExpiresAt
    )).ToListAsync(ct);

    public async Task<PharmacyDocumentResponse?> ReviewAsync(int pharmacyId, int id, PharmacyDocumentReviewRequest request, CancellationToken ct)
    {
        var document = await context.PharmacyDocuments
            .FirstOrDefaultAsync(d => d.Id == id && d.PharmacyId == pharmacyId, ct);

        if (document is null) return null;

        document.ReviewStatus = request.ReviewStatus;
        document.ReviewedByUserId = request.ReviewedByUserId;
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Document {DocumentId} for Pharmacy {PharmacyId} reviewed by User {ReviewerId} -> {ReviewStatus}",
            document.Id, document.PharmacyId, document.ReviewedByUserId, document.ReviewStatus);

        return await GetByPharmacyIdAsync(pharmacyId, id, ct);
    }
}
