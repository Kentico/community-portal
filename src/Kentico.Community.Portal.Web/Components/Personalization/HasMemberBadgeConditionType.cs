using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Members.Badges;
using Kentico.Community.Portal.Web.Membership;
using Kentico.PageBuilder.Web.Mvc.Personalization;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Identity;

[assembly: RegisterPersonalizationConditionType(
    identifier: HasMemberBadgeConditionType.IDENTIFIER,
    type: typeof(HasMemberBadgeConditionType),
    name: "Has member badge",
    Description = "Evaluates based on the the community member badges assigned to the current visitor.",
    IconClass = "icon-app-membership",
    Hint = "Display personalized experiences to visitors who have at least one of the selected community member badges")]

namespace Kentico.PageBuilder.Web.Mvc.Personalization;

public class HasMemberBadgeConditionType(
    IHttpContextAccessor contextAccessor,
    MemberBadgeService badgeService,
    UserManager<CommunityMember> userManager) : ConditionType
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Personalization.HasMemberBadge";

    /// <summary>
    /// Selected contact group code names.
    /// </summary>
    [ObjectSelectorComponent(
        MemberBadgeInfo.OBJECT_TYPE,
        Label = "Member badges",
        Order = 0,
        MaximumItems = 0)]
    public IEnumerable<ObjectRelatedItem> SelectedMemberBadges { get; set; } = [];

    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly MemberBadgeService badgeService = badgeService;
    private readonly UserManager<CommunityMember> userManager = userManager;

    public override bool Evaluate()
    {
        var context = contextAccessor.HttpContext;
        if (context is null)
        {
            return false;
        }

        var identity = context.User.Identity;

        if (identity is null || identity.IsAuthenticated)
        {
            return false;
        }

        var member = userManager
            .GetUserAsync(context.User)
            .GetAwaiter()
            .GetResult();

        if (member is null)
        {
            return false;
        }

        var assignedMemberBadgeNames = badgeService
            .GetSelectedBadgesFor(member.Id)
            .GetAwaiter()
            .GetResult()
            .Select(b => b.MemberBadgeCodeName);

        return SelectedMemberBadges
            .IntersectBy(assignedMemberBadgeNames, selected => selected.ObjectCodeName, StringComparer.OrdinalIgnoreCase)
            .Any();
    }
}
