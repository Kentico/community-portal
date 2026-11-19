using CMS.DataEngine;
using CMS.Membership;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Community.Portal.Core.Modules;

/// <summary>
/// Shared Member Role picker, backing both the <c>MemberRoleConditionType</c>
/// Page Builder personalization condition and the admin Member Roles list filter.
/// </summary>
public class MemberRoleDataProvider(IInfoProvider<MemberRoleInfo> roleProvider) : IGeneralSelectorDataProvider
{
    private readonly IInfoProvider<MemberRoleInfo> roleProvider = roleProvider;

    public async Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        var roles = await roleProvider
            .Get()
            .Columns(nameof(MemberRoleInfo.MemberRoleName), nameof(MemberRoleInfo.MemberRoleDisplayName))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var items = string.IsNullOrEmpty(searchTerm)
            ? roles.Select(r => new ObjectSelectorListItem<string> { Value = r.MemberRoleName, Text = r.MemberRoleDisplayName, IsValid = true })
            : roles.Where(r => r.MemberRoleDisplayName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                .Select(r => new ObjectSelectorListItem<string> { Value = r.MemberRoleName, Text = r.MemberRoleDisplayName, IsValid = true });

        return new PagedSelectListItems<string>
        {
            NextPageAvailable = false,
            Items = items,
        };
    }

    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(
        IEnumerable<string> selectedValues,
        CancellationToken cancellationToken)
    {
        var roles = await roleProvider
            .Get()
            .Columns(nameof(MemberRoleInfo.MemberRoleName), nameof(MemberRoleInfo.MemberRoleDisplayName))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return (selectedValues ?? []).Select(value =>
            roles.FirstOrDefault(r => string.Equals(value, r.MemberRoleName, StringComparison.OrdinalIgnoreCase)) is var role && role != null
                ? new ObjectSelectorListItem<string> { Value = role.MemberRoleName, Text = role.MemberRoleDisplayName, IsValid = true }
                : new ObjectSelectorListItem<string> { IsValid = false, Text = value, Value = value });
    }
}
