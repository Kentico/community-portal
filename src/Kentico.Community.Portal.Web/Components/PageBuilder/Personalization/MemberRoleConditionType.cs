using Kentico.PageBuilder.Web.Mvc.Personalization;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.ComponentIcons;
using CMS.DataEngine;
using CMS.Membership;

[assembly: RegisterPersonalizationConditionType(
    identifier: MemberRoleConditionType.IDENTIFIER,
    type: typeof(MemberRoleConditionType),
    name: "Member Role",
    Description = "Personalizes content based on member role.",
    IconClass = KenticoIcons.ID_CARDS,
    Hint = "Display to members who have at least one of the selected roles:")]

namespace Kentico.PageBuilder.Web.Mvc.Personalization;

public class MemberRoleConditionType(
    IHttpContextAccessor contextAccessor) : ConditionType
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Personalization.MemberRole";

    [GeneralSelectorComponent(
        dataProviderType: typeof(MemberRoleDataProvider),
        Label = "Member Roles",
        ExplanationText = "Members only need to belong to at least 1 of the selected roles.",
        Placeholder = "Select role(s)",
        Order = 0)]
    public IEnumerable<string> SelectedRoles { get; set; } = [];

    private readonly IHttpContextAccessor contextAccessor = contextAccessor;

    public override bool Evaluate()
    {
        var context = contextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (!SelectedRoles.Any())
        {
            return false;
        }

        return SelectedRoles.Any(context.User.IsInRole);
    }
}

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
