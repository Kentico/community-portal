using CMS.DataEngine;
using CMS.Membership;

namespace Kentico.Community.Portal.Web.Infrastructure;

public static class UserInfoExtensions
{
    /// <summary>
    /// Represents a non-human user used for automated content creation tasks.
    /// See: https://docs.kentico.com/changelog#refresh-may-19-2025
    /// </summary>
    public const string CONTENT_AUTHOR_USERNAME = "kentico-system-service";

    /// <summary>
    /// Returns the user associated with <see cref="CONTENT_AUTHOR_USERNAME"/>
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<UserInfo> GetPublicMemberContentAuthor(this IInfoProvider<UserInfo> provider)
    {
        var users = await provider.Get()
            .WhereEquals(nameof(UserInfo.UserName), CONTENT_AUTHOR_USERNAME)
            .GetEnumerableTypedResultAsync();

        return users.FirstOrDefault() ?? throw new Exception($"Could not retrieve user {CONTENT_AUTHOR_USERNAME} which is required for automated content creation.");
    }
}
