using System.Security.Claims;
using Kentico.Community.Portal.Web.Membership;

namespace Microsoft.AspNetCore.Identity;

public static class UserManagerExtensions
{
    /// <summary>
    /// Retrieves the current <see cref="CommunityMember" /> from the request, if the request is authenticated
    /// </summary>
    /// <param name="userManager"></param>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public static async Task<CommunityMember?> CurrentUser(this UserManager<CommunityMember> userManager, HttpContext httpContext)
    {
        string? userId = httpContext.User.FindFirstValue(userManager.Options.ClaimsIdentity.UserIdClaimType);

        if (userId is null)
        {
            return null;
        }

        return await userManager.FindByIdAsync(userId);
    }
}
