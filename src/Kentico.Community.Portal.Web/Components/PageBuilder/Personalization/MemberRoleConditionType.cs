using Kentico.PageBuilder.Web.Mvc.Personalization;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.ComponentIcons;
using Kentico.Community.Portal.Core.Modules;

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
