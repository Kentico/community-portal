using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(SupportRequestConfigurationListingPage),
    slug: "create",
    uiPageType: typeof(SupportRequestConfigurationCreatePage),
    name: "Create Configuration",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestConfigurationCreatePage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder, IPageUrlGenerator pageUrlGenerator)
    : CreatePage<SupportRequestConfigurationInfo, SupportRequestConfigurationEditPage>(formComponentMapper, formDataBinder, pageUrlGenerator)
{
}
