using CMS.DataEngine;
using CMS.Membership;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Community.Portal.Admin.Infrastructure;

/// <summary>
/// Selects enabled administration users by user name. Shared by every admin form that needs to
/// pick notification recipients.
/// </summary>
internal sealed class UserGeneralSelectorDataProvider(
    IInfoProvider<UserInfo> userProvider) : IGeneralSelectorDataProvider
{
    private readonly IInfoProvider<UserInfo> userProvider = userProvider;
    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Invalid", Value = string.Empty };

    public async Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        var query = userProvider.Get()
            .Columns(nameof(UserInfo.UserName))
            .WhereEquals(nameof(UserInfo.UserEnabled), true);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.WhereContains(nameof(UserInfo.UserName), searchTerm);
        }

        var users = await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return new PagedSelectListItems<string>
        {
            NextPageAvailable = false,
            Items = users.Select(u => new ObjectSelectorListItem<string> { IsValid = true, Text = u.UserName, Value = u.UserName }),
        };
    }

    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(
        IEnumerable<string> selectedValues,
        CancellationToken cancellationToken)
    {
        var users = await userProvider.Get()
            .Columns(nameof(UserInfo.UserName))
            .WhereEquals(nameof(UserInfo.UserEnabled), true)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return (selectedValues ?? []).Select(value =>
            users.FirstOrDefault(u => string.Equals(value, u.UserName, StringComparison.OrdinalIgnoreCase)) is { } user
                ? new ObjectSelectorListItem<string> { IsValid = true, Text = user.UserName, Value = user.UserName }
                : InvalidItem);
    }
}
