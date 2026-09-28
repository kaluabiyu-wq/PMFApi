using Microsoft.AspNetCore.Authorization;

namespace PmfApi.Api.Authorization;

public class PharmacyOwnerRequirement(bool allowSystemAdmin = false)
 : IAuthorizationRequirement
{
    public bool AllowSystemAdmin { get; } = allowSystemAdmin;
}