using Microsoft.AspNetCore.Authorization;

namespace PmfApi.Api.Authorization;

public class PharmacyOwnerRequirement(bool allowSystemAdmin = false,
bool requiredElevatedStaffRole = false)
 : IAuthorizationRequirement
{
    public bool AllowSystemAdmin { get; } = allowSystemAdmin;
    public bool RequireElevatedStaffRole {get;} = requiredElevatedStaffRole;
}