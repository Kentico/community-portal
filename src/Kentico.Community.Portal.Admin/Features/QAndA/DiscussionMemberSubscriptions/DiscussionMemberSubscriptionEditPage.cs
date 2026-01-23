using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberSubscriptions;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(DiscussionMemberSubscriptionSectionPage),
    slug: "edit",
    uiPageType: typeof(DiscussionMemberSubscriptionEditPage),
    name: "Edit",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberSubscriptions;

public class DiscussionMemberSubscriptionEditPage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder, IInfoProvider<MemberInfo> provider)
    : InfoEditPage<DiscussionMemberSubscriptionInfo>(formComponentMapper, formDataBinder)
{
    private readonly IInfoProvider<MemberInfo> provider = provider;

    [PageParameter(typeof(IntPageModelBinder))]
    public override int ObjectId { get; set; }

    protected override async Task<string> GetObjectDisplayName(DiscussionMemberSubscriptionInfo infoObject)
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
