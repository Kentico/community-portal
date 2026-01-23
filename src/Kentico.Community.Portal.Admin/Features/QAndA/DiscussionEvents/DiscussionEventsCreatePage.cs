using Kentico.Community.Portal.Admin.Features.QAndA.DiscussionEvents;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(DiscussionEventsListingPage),
    slug: "create",
    uiPageType: typeof(DiscussionEventsCreatePage),
    name: "Create Discussion Event",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.DiscussionEvents;

public class DiscussionEventsCreatePage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder, IPageLinkGenerator pageLinkGenerator)
    : CreatePage<DiscussionEventInfo, DiscussionEventsEditPage>(formComponentMapper, formDataBinder, pageLinkGenerator)
{
}
