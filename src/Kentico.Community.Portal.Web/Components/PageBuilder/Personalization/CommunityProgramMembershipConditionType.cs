using Kentico.Community.Portal.Core.Modules;
using Kentico.PageBuilder.Web.Mvc.Personalization;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterPersonalizationConditionType(
    identifier: CommunityProgramMembershipConditionType.IDENTIFIER,
    type: typeof(CommunityProgramMembershipConditionType),
    name: "Has community program membership",
    Description = "Evaluates based on the visitor's community program membership.",
    IconClass = "icon-app-membership",
    Hint = "Display personalized experiences to visitors who are Community Program members (MVPs or Community Leaders)")]

namespace Kentico.PageBuilder.Web.Mvc.Personalization;

public class CommunityProgramMembershipConditionType(
    IHttpContextAccessor contextAccessor) : ConditionType
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Personalization.CommunityProgramMembership";

    [GeneralSelectorComponent(
        dataProviderType: typeof(CommunityProgramMembershipDataProvider),
        Label = "Community Programs",
        ExplanationText = "Members only need to belong to at least 1 of the selected programs. If no programs are selected, any program member will match.",
        Placeholder = "Select program(s)",
        Order = 0)]
    public IEnumerable<string> Membership { get; set; } = [];

    private readonly IHttpContextAccessor contextAccessor = contextAccessor;

    public override bool Evaluate()
    {
        var context = contextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        bool isMvp = context.User.IsInRole(SystemMemberRoles.KenticoMVP.MemberRoleName);
        bool isCommunityLeader = context.User.IsInRole(SystemMemberRoles.KenticoCommunityLeader.MemberRoleName);

        if (!Membership.Any())
        {
            return isMvp || isCommunityLeader;
        }

        return (isMvp && Membership.Contains(nameof(CommunityProgramMembership.MVP), StringComparer.OrdinalIgnoreCase))
            || (isCommunityLeader && Membership.Contains(nameof(CommunityProgramMembership.CommunityLeader), StringComparer.OrdinalIgnoreCase));
    }
}

public class CommunityProgramMembershipDataProvider : IGeneralSelectorDataProvider
{
    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Invalid", Value = "" };

    private static readonly IReadOnlyList<ObjectSelectorListItem<string>> allItems =
    [
        new() { Value = nameof(CommunityProgramMembership.MVP), Text = "MVP", IsValid = true },
        new() { Value = nameof(CommunityProgramMembership.CommunityLeader), Text = "Community Leader", IsValid = true },
    ];

    public Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        var items = string.IsNullOrEmpty(searchTerm)
            ? allItems
            : allItems.Where(i => i.Text.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(new PagedSelectListItems<string>()
        {
            NextPageAvailable = false,
            Items = items,
        });
    }

    public Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(IEnumerable<string> selectedValues, CancellationToken cancellationToken) =>
        Task.FromResult((selectedValues ?? []).Select(GetSelectedItemByValue));

    private static ObjectSelectorListItem<string> GetSelectedItemByValue(string value) =>
        allItems.FirstOrDefault(i => string.Equals(i.Value, value, StringComparison.OrdinalIgnoreCase))
            ?? InvalidItem;
}
