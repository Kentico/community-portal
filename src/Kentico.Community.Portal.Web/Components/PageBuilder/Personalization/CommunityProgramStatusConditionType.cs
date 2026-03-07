using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Web.Membership;
using Kentico.PageBuilder.Web.Mvc.Personalization;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Identity;

[assembly: RegisterPersonalizationConditionType(
    identifier: CommunityProgramStatusConditionType.IDENTIFIER,
    type: typeof(CommunityProgramStatusConditionType),
    name: "Has community program status",
    Description = "Evaluates based on the visitor's community program membership status.",
    IconClass = "icon-app-membership",
    Hint = "Display personalized experiences to visitors who are Community Program members (MVPs or Community Leaders)")]

namespace Kentico.PageBuilder.Web.Mvc.Personalization;

public class CommunityProgramStatusConditionType(
    IHttpContextAccessor contextAccessor,
    UserManager<CommunityMember> userManager) : ConditionType
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Personalization.CommunityProgramStatus";

    [GeneralSelectorComponent(
        dataProviderType: typeof(CommunityProgramMembershipDataProvider),
        Label = "Community Programs",
        ExplanationText = "Members only need to belong to at least 1 of the selected programs. If no programs are selected, any program member will match.",
        Placeholder = "Select program(s)",
        Order = 0)]
    public IEnumerable<string> Membership { get; set; } = [];

    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly UserManager<CommunityMember> userManager = userManager;

    public override bool Evaluate()
    {
        var context = contextAccessor.HttpContext;
        if (context is null)
        {
            return false;
        }

        var identity = context.User.Identities.FirstOrDefault();
        if (identity is null || !identity.IsAuthenticated)
        {
            return false;
        }

        var member = userManager.GetUserAsync(context.User).GetAwaiter().GetResult();
        if (member is null)
        {
            return false;
        }

        if (!Membership.Any())
        {
            return member.IsCommunityProgramMember;
        }

        return Membership.Contains(member.ProgramStatus.ToString(), StringComparer.OrdinalIgnoreCase);
    }
}

public class CommunityProgramMembershipDataProvider : IGeneralSelectorDataProvider
{
    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Invalid", Value = "" };

    private static readonly IReadOnlyList<ObjectSelectorListItem<string>> allItems =
    [
        new() { Value = nameof(ProgramStatuses.MVP), Text = "MVP", IsValid = true },
        new() { Value = nameof(ProgramStatuses.CommunityLeader), Text = "Community Leader", IsValid = true },
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
