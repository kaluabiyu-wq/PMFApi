using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PmfApi.Domain.Entities;
using PmfApi.Infrastructure.Persistence;

namespace PmfApi.Api.Authorization;

public class PharmacyOwnerHandler(PmfDbContext dbContext)
    : AuthorizationHandler<PharmacyOwnerRequirement, Pharmacy>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PharmacyOwnerRequirement requirement,
        Pharmacy resource)
    {
        if (requirement.AllowSystemAdmin && context.User.IsInRole(RoleNames.SystemAdmin))
        {
            context.Succeed(requirement);
            return;
        }

        
        if (requirement.RequireElevatedStaffRole
            && !context.User.IsInRole(RoleNames.PharmacyStaff)
            && !context.User.IsInRole(RoleNames.PharmacyAdmin))
        {
             return;
        }
        

         var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var isActiveStaffOfPharmacy = await dbContext.PharmacyStaff
            .AsNoTracking()
            .AnyAsync(s => s.UserId == userId
                        && s.PharmacyId == resource.Id
                        && s.IsActive);

        if (isActiveStaffOfPharmacy)
        {
            context.Succeed(requirement);
        }

       
    }
}