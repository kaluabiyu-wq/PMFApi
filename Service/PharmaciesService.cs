


using Microsoft.EntityFrameworkCore;
using PmfApi.Data;
using PmfApi.Dto;
using PmfApi.Entities;

namespace PmfApi.Service;

public class PharamaciesSerivce(PmfDbContext context, ILogger<PharamaciesSerivce> logger)
: IPharmaciesService
{
    public Task<PharmacyResponse?> GetByIdAsync(int id, CancellationToken ct) =>
    context.Pharmacies.AsNoTracking()
    .Where(p => p.Id == id)
    .Select(p => new PharmacyResponse (
        p.Id,p.Name,p.LicenceNumber,p.LocationId,p.IsVerified,
        p.ReliablityScore,p.FreshnessThreshold,
        p.LastInventoryUpdateAt,p.RegisteredAt

    )).FirstOrDefaultAsync(ct);

    public async Task<PharmacyResponse> CreateAsync(PharmacyRequest request,CancellationToken ct)
    {
        var pharmacies = new Pharmacy
        {
            Name = request.Name,
            LicenceNumber = request.LicenseNumber,
            LocationId = request.LocationId,
            IsVerified = request.IsVerified
            
        };
        context.Pharmacies.Add(pharmacies);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created Pharmacies {PhramacyId} {Name} {LicenceNumber} {LocationId}",
            pharmacies.Id,pharmacies.Name,pharmacies.LicenceNumber,pharmacies.LocationId);

        return (await GetByIdAsync(pharmacies.Id,ct))!;
    }


public async Task<PagedResponse<PharmacyResponse>> GetPharmacyAsync(PagedRequest request, CancellationToken ct)
{
    IQueryable<Pharmacy> query = context.Pharmacies.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        query = query.Where(c => EF.Functions.ILike(c.LicenceNumber, $"%{request.Search}%")
                               || EF.Functions.ILike(c.Name, $"%{request.Search}%"));
    }

    var totalCount = await query.CountAsync(ct);

    IOrderedQueryable<Pharmacy> sortedQuery = request.OrderBy switch
    {
        "Name" => request.Descending
            ? query.OrderByDescending(c => c.Name)
            : query.OrderBy(c => c.Name),
        "LicenceNumber" => request.Descending
            ? query.OrderByDescending(c => c.LicenceNumber)
            : query.OrderBy(c => c.LicenceNumber),
        "ReliabilityScore" => request.Descending
            ? query.OrderByDescending(c => c.ReliablityScore)
            : query.OrderBy(c => c.ReliablityScore),
        _ =>  request.Descending
            ? query.OrderByDescending(c => c.Email)
            : query.OrderBy(c => c.Email)
    };

    var items = await sortedQuery
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(c => new PharmacyResponse(c.Id, c.Name, c.LicenceNumber,
        c.LocationId,c.IsVerified,c.ReliablityScore,
        c.FreshnessThreshold,c.LastInventoryUpdateAt,c.RegisteredAt))
        .ToListAsync(ct);

    return new PagedResponse<PharmacyResponse>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}

}