using Microsoft.AspNetCore.Authorization;

namespace PmfApi.Api.Authorization;

public class PharmacyDocumentReviewHandler
    : AuthorizationHandler<PharmacyDocumentReviewRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PharmacyDocumentReviewRequirement requirement)
    {
        if (context.User.IsInRole(RoleNames.SystemAdmin))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}