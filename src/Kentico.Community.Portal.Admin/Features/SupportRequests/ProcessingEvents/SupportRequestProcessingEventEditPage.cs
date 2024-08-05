using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(SupportRequestProcessingEventSectionPage),
    slug: "edit",
    uiPageType: typeof(SupportRequestProcessingEventEditPage),
    name: "Edit Event",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class SupportRequestProcessingEventEditPage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder) : InfoEditPage<SupportRequestProcessingEventInfo>(formComponentMapper, formDataBinder)
{
    [PageParameter(typeof(IntPageModelBinder))]
    public override int ObjectId { get; set; }
}
