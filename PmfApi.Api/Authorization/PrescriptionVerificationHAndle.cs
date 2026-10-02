using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PmfApi.Domain.Entities;
using PmfApi.Infrastructure.Persistence;

namespace PmfApi.Api.Authorization;

public class PrescriptionVerificationHandler(PmfDbContext dbContext)
    : AuthorizationHandler<PrescriptionVerificationRequirement, Prescription>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PrescriptionVerificationRequirement requirement,
        Prescription resource)
    {
        if (context.User.IsInRole(RoleNames.SystemAdmin))
        {
            context.Succeed(requirement);
            return;
        }

        if (!context.User.IsInRole(RoleNames.PharmacyStaff)
            && !context.User.IsInRole(RoleNames.PharmacyAdmin))
        {
            return;
        }

        if (!int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return;
        }

        
        var isStaffOfOrderPharmacy = await dbContext.PharmacyStaff
            .AsNoTracking()
            .AnyAsync(s => s.UserId == userId
                        && s.IsActive
                        && dbContext.Orders.Any(o => o.Id == resource.OrderId
                                                  && o.PharmacyId == s.PharmacyId));

        if (isStaffOfOrderPharmacy)
        {
            context.Succeed(requirement);
        }
    }
}