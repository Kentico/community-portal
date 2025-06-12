using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Membership;
using Kentico.PageBuilder.Web.Mvc.Personalization;
using Microsoft.AspNetCore.Identity;

[assembly: RegisterPersonalizationConditionType(
    identifier: IsMVPConditionType.IDENTIFIER,
    type: typeof(IsMVPConditionType),
    name: "Is MVP",
    Description = "Evaluates based on the visitor's membership status.",
    IconClass = "icon-app-membership",
    Hint = "Display personalized experiences to visitors who are MVPs")]

namespace Kentico.PageBuilder.Web.Mvc.Personalization;

public class IsMVPConditionType(
    IHttpContextAccessor contextAccessor,
    UserManager<CommunityMember> userManager,
    IMemberBadgeMemberInfoProvider memberBadgeMemberProvider,
    IMemberBadgeInfoProvider memberBadgeProvider) : ConditionType
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Personalization.IsMVP";

    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IMemberBadgeMemberInfoProvider memberBadgeMemberProvider = memberBadgeMemberProvider;
    private readonly IMemberBadgeInfoProvider memberBadgeProvider = memberBadgeProvider;

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

        var badges = memberBadgeProvider.GetAllMemberBadgesCached().GetAwaiter().GetResult();
        int mvpBadgeID = badges
            .TryFirst(badge => string.Equals(badge.MemberBadgeCodeName, "KenticoMVP", StringComparison.OrdinalIgnoreCase))
            .Map(b => b.MemberBadgeID)
            .GetValueOrDefault();
        var badgeRelationships = memberBadgeMemberProvider.GetAllMemberBadgeRelationshipsCached().GetAwaiter().GetResult();

        return badgeRelationships.HasEntry(member.Id, mvpBadgeID);
    }
}
