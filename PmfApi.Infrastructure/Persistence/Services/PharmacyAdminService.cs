using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Application.Dtos;
using PmfApi.Domain.Entities;
using PmfApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PmfApi.Infrastructure.Persistence.Services;

public class PharmacyAdminService(PmfDbContext context, ILogger<PharmacyAdminService> logger)
: IPharmacyAdminService
{
    public async Task<PagedResponse<PharmacyAdminSummaryResponse>> GetAllAsync(PharmacyAdminQuery query, CancellationToken ct)
    {
        IQueryable<Pharmacy> pharmacies = context.Pharmacies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            pharmacies = pharmacies.Where(p => EF.Functions.ILike(p.Name, $"%{query.Search}%")
                                             || EF.Functions.ILike(p.LicenceNumber, $"%{query.Search}%"));
        }

        if (query.IsVerified is not null)
            pharmacies = pharmacies.Where(p => p.IsVerified == query.IsVerified);

        if (query.IsActive is not null)
            pharmacies = pharmacies.Where(p => p.IsActive == query.IsActive);

        if (string.Equals(query.Freshness, "Stale", StringComparison.OrdinalIgnoreCase))
            pharmacies = pharmacies.Where(p => p.LastInventoryUpdateAt.AddHours(p.FreshnessThreshold) < DateTime.UtcNow);
        else if (string.Equals(query.Freshness, "Fresh", StringComparison.OrdinalIgnoreCase))
            pharmacies = pharmacies.Where(p => p.LastInventoryUpdateAt.AddHours(p.FreshnessThreshold) >= DateTime.UtcNow);

        var totalCount = await pharmacies.CountAsync(ct);

        IOrderedQueryable<Pharmacy> sorted = query.OrderBy switch
        {
            "Name" => query.Descending
                ? pharmacies.OrderByDescending(p => p.Name)
                : pharmacies.OrderBy(p => p.Name),
            "LicenceNumber" => query.Descending
                ? pharmacies.OrderByDescending(p => p.LicenceNumber)
                : pharmacies.OrderBy(p => p.LicenceNumber),
            "ReliabilityScore" => query.Descending
                ? pharmacies.OrderByDescending(p => p.ReliablityScore)
                : pharmacies.OrderBy(p => p.ReliablityScore),
            _ => query.Descending
                ? pharmacies.OrderByDescending(p => p.RegisteredAt)
                : pharmacies.OrderBy(p => p.RegisteredAt)
        };

        var rows = await sorted
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.LicenceNumber,
                p.IsVerified,
                p.IsActive,
                p.ReliablityScore,
                p.FreshnessThreshold,
                p.LastInventoryUpdateAt,
                p.RegisteredAt,
                ActiveStaffCount = p.PharmacyStaff.Count(s => s.IsActive),
                PendingDocumentCount = p.Documents.Count(d => d.ReviewStatus == ReviewStatus.Pending)
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new PharmacyAdminSummaryResponse(
            r.Id, r.Name, r.LicenceNumber, r.IsVerified, r.IsActive, r.ReliablityScore,
            ComputeFreshnessStatus(r.LastInventoryUpdateAt, r.FreshnessThreshold),
            r.ActiveStaffCount, r.PendingDocumentCount, r.LastInventoryUpdateAt, r.RegisteredAt
        )).ToList();

        return new PagedResponse<PharmacyAdminSummaryResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<PharmacyAdminProfileResponse?> GetProfileAsync(int pharmacyId, CancellationToken ct)
    {
        var p = await context.Pharmacies.AsNoTracking()
            .Where(p => p.Id == pharmacyId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.LicenceNumber,
                p.PhoneNumber,
                p.Email,
                p.IsVerified,
                p.IsActive,
                p.ReliablityScore,
                p.FreshnessThreshold,
                p.LastInventoryUpdateAt,
                p.RegisteredAt,
                p.LocationId,
                TotalStaffCount = p.PharmacyStaff.Count(),
                ActiveStaffCount = p.PharmacyStaff.Count(s => s.IsActive),
                InventoryItemCount = p.Inventories.Count(),
                PendingDocumentCount = p.Documents.Count(d => d.ReviewStatus == ReviewStatus.Pending),
                ApprovedDocumentCount = p.Documents.Count(d => d.ReviewStatus == ReviewStatus.Approved),
                RejectedDocumentCount = p.Documents.Count(d => d.ReviewStatus == ReviewStatus.Rejected),
                ReviewCount = p.Reviews.Count(),
                AverageRating = p.Reviews.Any() ? (double?)p.Reviews.Average(r => r.Rating) : null
            })
            .FirstOrDefaultAsync(ct);

        if (p is null) return null;

        return new PharmacyAdminProfileResponse(
            p.Id, p.Name, p.LicenceNumber, p.PhoneNumber, p.Email,
            p.IsVerified, p.IsActive, p.ReliablityScore,
            ComputeFreshnessStatus(p.LastInventoryUpdateAt, p.FreshnessThreshold),
            p.FreshnessThreshold, p.LastInventoryUpdateAt, p.RegisteredAt, p.LocationId,
            p.TotalStaffCount, p.ActiveStaffCount, p.InventoryItemCount,
            p.PendingDocumentCount, p.ApprovedDocumentCount, p.RejectedDocumentCount,
            p.ReviewCount, p.AverageRating
        );
    }

    public async Task<PharmacyResponse?> UpdateAsync(int pharmacyId, PharmacyUpdateRequest request, CancellationToken ct)
    {
        var pharmacy = await context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyId, ct);
        if (pharmacy is null) return null;

        pharmacy.Name = request.Name;
        pharmacy.LicenceNumber = request.LicenseNumber;
        pharmacy.LocationId = request.LocationId;
        pharmacy.PhoneNumber = request.PhoneNumber;
        pharmacy.Email = request.Email;
        pharmacy.FreshnessThreshold = request.FreshnessThreshold;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Admin updated Pharmacy {PharmacyId} profile ({Name}, {LicenceNumber})",
            pharmacy.Id, pharmacy.Name, pharmacy.LicenceNumber);

        return await GetResponseAsync(pharmacy.Id, ct);
    }

    public async Task<PharmacyResponse?> SetStatusAsync(int pharmacyId, PharmacyStatusUpdateRequest request, CancellationToken ct)
    {
        var pharmacy = await context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyId, ct);
        if (pharmacy is null) return null;

        pharmacy.IsVerified = request.IsVerified;
        pharmacy.IsActive = request.IsActive;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin set Pharmacy {PharmacyId} status to IsVerified={IsVerified} IsActive={IsActive} ({Reason})",
            pharmacy.Id, pharmacy.IsVerified, pharmacy.IsActive, request.Reason ?? "no reason given");

        return await GetResponseAsync(pharmacy.Id, ct);
    }

    public async Task<bool> DeleteAsync(int pharmacyId, CancellationToken ct)
    {
        var pharmacy = await context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyId, ct);
        if (pharmacy is null) return false;

          pharmacy.IsActive = false;
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Admin soft-deleted Pharmacy {PharmacyId} ({Name})", pharmacy.Id, pharmacy.Name);
        return true;
    }

    private Task<PharmacyResponse?> GetResponseAsync(int id, CancellationToken ct) =>
        context.Pharmacies.AsNoTracking()
        .Where(p => p.Id == id)
        .Select(p => new PharmacyResponse(
            p.Id, p.Name, p.LicenceNumber, p.LocationId, p.IsVerified,
            p.ReliablityScore, p.FreshnessThreshold, p.LastInventoryUpdateAt, p.RegisteredAt
        )).FirstOrDefaultAsync(ct);

    private static string ComputeFreshnessStatus(DateTime lastInventoryUpdateAt, int freshnessThresholdHours) =>
        DateTime.UtcNow - lastInventoryUpdateAt > TimeSpan.FromHours(freshnessThresholdHours) ? "Stale" : "Fresh";
}