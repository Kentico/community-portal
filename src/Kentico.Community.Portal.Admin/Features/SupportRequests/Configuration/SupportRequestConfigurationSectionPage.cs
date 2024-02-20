using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(SupportRequestConfigurationListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(SupportRequestConfigurationSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestConfigurationSectionPage : EditSectionPage<SupportRequestConfigurationInfo>
{
    protected override Task<string> GetObjectDisplayName(BaseInfo infoObject) => Task.FromResult("Module Configuration");
}
