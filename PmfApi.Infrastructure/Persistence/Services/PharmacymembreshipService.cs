using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PmfApi.Application.Abstractions;

namespace PmfApi.Infrastructure.Persistence.Services;

public class PharmacyMembershipService(PmfDbContext context, ILogger<PharmacyMembershipService> logger)
    : IPharmacyMembershipService
{
    public async Task<IReadOnlyList<int>> GetPharmacyIdsForUserAsync(
        string userId, CancellationToken ct = default)
    {
       
        if (!int.TryParse(userId, out var id))
        {
            logger.LogWarning("Membership lookup received a non-numeric user id {UserId}", userId);
            return Array.Empty<int>();
        }

       
        return await context.PharmacyStaff
            .AsNoTracking()
            .Where(s => s.UserId == id
                        && s.IsActive
                        && s.Pharmacy.IsActive)
            .Select(s => s.PharmacyId)
            .Distinct()
            .ToListAsync(ct);
    }
}