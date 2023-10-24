using System.Security.Claims;
using CMS.Membership;
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

    /// <summary>
    /// True if the current <see cref="CommunityMember" /> can manage content on the live site
    /// </summary>
    /// <param name="userManager"></param>
    /// <param name="httpContext"></param>
    /// <param name="userInfoProvider"></param>
    /// <returns></returns>
    public static async Task<bool> CanManageContent(this UserManager<CommunityMember> userManager, HttpContext httpContext, IUserInfoProvider userInfoProvider)
    {
        var currentUser = await userManager.CurrentUser(httpContext);

        if (currentUser is null)
        {
            return false;
        }

        var cmsUser = (await userInfoProvider.Get()
            .WhereEquals(nameof(UserInfo.Email), currentUser.Email)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (cmsUser is null)
        {
            return false;
        }

        return cmsUser.IsInRole("ContentManager");
    }

    public static async Task<bool> CanManageContent(this UserManager<CommunityMember> _, CommunityMember? communityMember, IUserInfoProvider userInfoProvider)
    {
        if (communityMember is null)
        {
            return false;
        }

        var cmsUser = (await userInfoProvider.Get()
            .WhereEquals(nameof(UserInfo.Email), communityMember.Email)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (cmsUser is null)
        {
            return false;
        }

        return cmsUser.IsInRole("ContentManager");
    }
}
