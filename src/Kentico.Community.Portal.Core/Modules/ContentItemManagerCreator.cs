using System.Security.Claims;
using CMS.DataEngine;
using CMS.Membership;
using CMS.Websites;
using Kentico.Membership;
using Microsoft.AspNetCore.Http;

namespace Kentico.Community.Portal.Core.Modules;

public class ContentItemManagerCreator(
    IHttpContextAccessor contextAccessor,
    IInfoProvider<UserInfo> userProvider,
    AdminUserManager adminUserManager,
    IWebPageManagerFactory webPageManagerFactory,
    IContentItemManagerFactory contentItemManagerfactory)
{
    public async Task<IContentItemManager> GetContentItemManager()
    {
        /*
         * Scheduled posts are published in a background thread
         * which means there's no HttpContext
         */
        if (contextAccessor.HttpContext?.User is not ClaimsPrincipal principal)
        {
            var contentAuthor = await userProvider.GetPublicMemberContentAuthor();
            return contentItemManagerfactory.Create(contentAuthor.UserID);
        }

        var adminUser = await adminUserManager.GetUserAsync(principal);

        if (adminUser is null)
        {
            var contentAuthor = await userProvider.GetPublicMemberContentAuthor();
            return contentItemManagerfactory.Create(contentAuthor.UserID);
        }

        return contentItemManagerfactory.Create(adminUser.UserID);
    }

    public async Task<IWebPageManager> GetWebPageItemManager(int websiteChannelID)
    {
        /*
         * Scheduled posts are published in a background thread
         * which means there's no HttpContext
         */
        if (contextAccessor.HttpContext?.User is not ClaimsPrincipal principal)
        {
            var contentAuthor = await userProvider.GetPublicMemberContentAuthor();
            return webPageManagerFactory.Create(websiteChannelID, contentAuthor.UserID);
        }

        var adminUser = await adminUserManager.GetUserAsync(principal);

        if (adminUser is null)
        {
            var contentAuthor = await userProvider.GetPublicMemberContentAuthor();
            return webPageManagerFactory.Create(websiteChannelID, contentAuthor.UserID);
        }

        return webPageManagerFactory.Create(websiteChannelID, adminUser.UserID);
    }
}
