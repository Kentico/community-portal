using Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberSubscriptions;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(DiscussionMemberSubscriptionListingPage),
    slug: "create",
    uiPageType: typeof(DiscussionMemberSubscriptionCreatePage),
    name: "Create",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberSubscriptions;

public class DiscussionMemberSubscriptionCreatePage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder, IPageLinkGenerator pageLinkGenerator)
    : CreatePage<DiscussionMemberSubscriptionInfo, DiscussionMemberSubscriptionEditPage>(formComponentMapper, formDataBinder, pageLinkGenerator)
{
}
