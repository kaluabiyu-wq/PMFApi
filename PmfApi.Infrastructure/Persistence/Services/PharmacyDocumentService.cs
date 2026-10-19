using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PmfApi.Infrastructure.Persistence.Services;

public class PharmacyDocumentService(
    PmfDbContext context,
    IFileStorage fileStorage,
    ILogger<PharmacyDocumentService> logger)
: IPharmacyDocumentService
{
    private const string StorageFolder = "pharmacy-documents/";

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

    public Task<PharmacyDocumentResponse> CreateFromUploadAsync(
        int pharmacyId, DocumentType type, string storedPath, DateTime? expiresAt, CancellationToken ct) =>
        CreateAsync(pharmacyId, new PharmacyDocumentRequest
        {
            DocumentType = type,
            FileUrl = storedPath,
            ExpiresAt = expiresAt,
        }, ct);

    public async Task<PharmacyDocumentResponse?> ReplaceFileAsync(
        int pharmacyId, int id, string storedPath, DateTime? expiresAt, CancellationToken ct)
    {
        var document = await context.PharmacyDocuments
            .FirstOrDefaultAsync(d => d.Id == id && d.PharmacyId == pharmacyId, ct);
        if (document is null) return null;

        var previousFile = document.FileUrl;

        document.FileUrl = storedPath;
        document.UploadedAt = DateTime.UtcNow;
        document.ExpiresAt = expiresAt ?? document.ExpiresAt;


        document.ReviewStatus = ReviewStatus.Pending;
        document.ReviewedByUserId = null;
        document.ReviewNote = null;

        await context.SaveChangesAsync(ct);

        if (previousFile.StartsWith(StorageFolder, StringComparison.Ordinal)
            && !previousFile.Contains("..", StringComparison.Ordinal))
        {
            try { fileStorage.Delete(previousFile); }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete replaced file {Path} of document {DocumentId}", previousFile, id);
            }
        }

        logger.LogInformation("Replaced file of document {DocumentId} for Pharmacy {PharmacyId}; back to Pending",
            document.Id, document.PharmacyId);

        return await GetByPharmacyIdAsync(pharmacyId, id, ct);
    }

    public Task<PharmacyDocumentResponse?> GetByPharmacyIdAsync(int pharmacyId, int id, CancellationToken ct) =>
    context.PharmacyDocuments.AsNoTracking()
    .Where(d => d.Id == id && d.PharmacyId == pharmacyId)
    .Select(d => new PharmacyDocumentResponse(
        d.Id, d.PharmacyId, d.DocumentType, d.FileUrl, d.UploadedAt,
        d.ReviewedByUserId, d.ReviewStatus, d.ExpiresAt, d.ReviewNote
    )).FirstOrDefaultAsync(ct);

    public Task<List<PharmacyDocumentResponse>> GetByPharmacyAsync(int pharmacyId, CancellationToken ct) =>
    context.PharmacyDocuments.AsNoTracking()
    .Where(d => d.PharmacyId == pharmacyId)
    .OrderByDescending(d => d.UploadedAt)
    .Select(d => new PharmacyDocumentResponse(
        d.Id, d.PharmacyId, d.DocumentType, d.FileUrl, d.UploadedAt,
        d.ReviewedByUserId, d.ReviewStatus, d.ExpiresAt, d.ReviewNote
    )).ToListAsync(ct);

    public async Task<PharmacyDocumentResponse?> ReviewAsync(int pharmacyId, int id, int reviewerId, PharmacyDocumentReviewRequest request, CancellationToken ct)
    {
        var document = await context.PharmacyDocuments
            .FirstOrDefaultAsync(d => d.Id == id && d.PharmacyId == pharmacyId, ct);

        if (document is null) return null;

        var previousStatus = document.ReviewStatus;
        var reason = request.Reason?.Trim();

        document.ReviewStatus = request.ReviewStatus;
        document.ReviewedByUserId = reviewerId;


        document.ReviewNote = request.ReviewStatus == ReviewStatus.Rejected && !string.IsNullOrEmpty(reason)
            ? reason
            : null;

        if (request.ReviewStatus != previousStatus && request.ReviewStatus != ReviewStatus.Pending)
        {
            var approved = request.ReviewStatus == ReviewStatus.Approved;

            await AlertWriter.ForPharmacyStaffAsync(
                context, document.PharmacyId,
                approved ? AlertEventType.DocumentApproved : AlertEventType.DocumentRejected,
                AlertReferenceTables.PharmacyDocuments, document.Id,
                approved
                    ? $"Your {document.DocumentType} document was approved."
                    : string.IsNullOrEmpty(reason)
                        ? $"Your {document.DocumentType} document was rejected."
                        : $"Your {document.DocumentType} document was rejected: {reason}", ct);
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Document {DocumentId} for Pharmacy {PharmacyId} reviewed by User {ReviewerId} -> {ReviewStatus}",
            document.Id, document.PharmacyId, document.ReviewedByUserId, document.ReviewStatus);

        return await GetByPharmacyIdAsync(pharmacyId, id, ct);
    }
}