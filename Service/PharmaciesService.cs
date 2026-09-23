


using Microsoft.EntityFrameworkCore;
using PmfApi.Data;
using PmfApi.Dto;
using PmfApi.Entities;

namespace PmfApi.Service;

public class PharamaciesSerivce(PmfDbContext context, ILogger<PharamaciesSerivce> logger)
: IPharmaciesService
{
    public Task<PharmacyResponse?> GetBylicenceAsync(string licenseNumber, CancellationToken ct) =>
    context.Pharmacies.AsNoTracking()
    .Where(p => p.LicenceNumber == licenseNumber)
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

        return (await GetBylicenceAsync(pharmacies.LicenceNumber,ct))!;
    }

}