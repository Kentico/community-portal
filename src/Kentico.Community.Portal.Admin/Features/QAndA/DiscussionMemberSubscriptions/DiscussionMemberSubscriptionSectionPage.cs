using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberSubscriptions;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(DiscussionMemberSubscriptionListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(DiscussionMemberSubscriptionSectionPage),
    name: "Section",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberSubscriptions;

public class DiscussionMemberSubscriptionSectionPage(IInfoProvider<MemberInfo> provider) : EditSectionPage<DiscussionMemberSubscriptionInfo>
{
    private readonly IInfoProvider<MemberInfo> provider = provider;

    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is not DiscussionMemberSubscriptionInfo subscription)
        {
            return await base.GetObjectDisplayName(infoObject);
        }

        var member = (await provider.Get()
            .WhereEquals(nameof(MemberInfo.MemberID), subscription.DiscussionMemberSubscriptionMemberID)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (member is null)
        {
            return await base.GetObjectDisplayName(infoObject);
        }

        return $"{member.MemberEmail} - {subscription.DiscussionMemberSubscriptionWebPageItemID}";
    }
}
