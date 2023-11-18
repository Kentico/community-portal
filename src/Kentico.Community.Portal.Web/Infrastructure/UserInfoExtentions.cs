using CMS.DataEngine;
using CMS.Membership;

namespace Kentico.Community.Portal.Web.Infrastructure;

public static class UserInfoExtensions
{
    public const string CONTENT_AUTHOR_USERNAME = "membership_author";

    public static async Task<UserInfo> GetPublicMemberContentAuthor(this IInfoProvider<UserInfo> provider)
    {
        var users = await provider.Get()
            .WhereEquals(nameof(UserInfo.UserName), CONTENT_AUTHOR_USERNAME)
            .GetEnumerableTypedResultAsync();

        return users.FirstOrDefault() ?? throw new Exception($"Could not retrieve user {CONTENT_AUTHOR_USERNAME}.");
    }
}
