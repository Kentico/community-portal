using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(SupportRequestConfigurationSectionPage),
    slug: "edit",
    uiPageType: typeof(SupportRequestConfigurationEditPage),
    name: "Edit Configuration",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestConfigurationEditPage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder) : InfoEditPage<SupportRequestConfigurationInfo>(formComponentMapper, formDataBinder)
{
    // Property that holds the ID of the edited user.
    // Needs to be decorated with the PageParameter attribute to propagate
    // user ID from the request path to the configuration of the page.
    [PageParameter(typeof(IntPageModelBinder))]
    public override int ObjectId { get; set; }
}
