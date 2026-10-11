using Microsoft.EntityFrameworkCore;
using PmfApi.Application.Dtos;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

public class PharmacyDashboardService(PmfDbContext context) : IPharmacyDashboardService
{
    public async Task<PharmacyDashboardResponse?> GetAsync(int pharmacyId, CancellationToken ct)
    {
        var pharmacy = await context.Pharmacies
            .AsNoTracking()
            .Where(p => p.Id == pharmacyId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.IsVerified,
                p.ReliablityScore,          // (sic) spelling as in the entity
                p.FreshnessThreshold,
                p.LastInventoryUpdateAt,
                HasRejectedDocument = p.Documents.Any(d => d.ReviewStatus == ReviewStatus.Rejected),
            })
            .FirstOrDefaultAsync(ct);

        if (pharmacy is null) return null;

        var itemsListed = await context.Inventories
            .CountAsync(i => i.PharmacyId == pharmacyId, ct);

        var outOfStock = await context.Inventories
            .CountAsync(i => i.PharmacyId == pharmacyId
                          && (i.Status.ToLower() == "outofstock" || i.Status.ToLower() == "out of stock"), ct);

        var feedback = context.UserFeedbacks
            .Join(context.Inventories.Where(i => i.PharmacyId == pharmacyId),
                  f => f.InventoryId, i => i.Id, (f, _) => f);

        var feedbackCount = await feedback.CountAsync(ct);
        var unavailableReports = await feedback.CountAsync(f => !f.WasMedicineAvailable, ct);

        var reviews = context.Reviews
    .AsNoTracking()
    .Where(r => r.PharmacyId == pharmacyId);

       var reviewCount = await reviews.CountAsync(ct);
       var averageRating = await reviews
    .Select(r => (double?)r.Rating)
    .AverageAsync(ct);         

      var verification = pharmacy.IsVerified ? "approved"
                 : pharmacy.HasRejectedDocument ? "rejected"
                 : "pending";

     var favoritesCount = await context.Favorites
    .CountAsync(f => f.PharmacyId == pharmacyId, ct);

return new PharmacyDashboardResponse(
    pharmacy.Id, pharmacy.Name, verification, pharmacy.ReliablityScore,
    pharmacy.FreshnessThreshold, itemsListed, outOfStock,
    feedbackCount, unavailableReports, pharmacy.LastInventoryUpdateAt,
    averageRating, reviewCount,favoritesCount);
    }

}