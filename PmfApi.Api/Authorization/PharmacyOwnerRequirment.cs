using Microsoft.AspNetCore.Authorization;

namespace PmfApi.Api.Authorization;

public class PharmacyOwnerRequirement(bool allowPlatformAdmin = false,
bool requiredElevatedStaffRole = false)
 : IAuthorizationRequirement
{
    public bool AllowPlatformAdmin { get; } = allowPlatformAdmin;
    public bool RequireElevatedStaffRole {get;} = requiredElevatedStaffRole;
}