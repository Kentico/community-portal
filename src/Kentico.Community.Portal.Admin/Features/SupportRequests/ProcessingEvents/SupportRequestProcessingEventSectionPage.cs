using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(SupportRequestProcessingEventListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(SupportRequestProcessingEventSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestProcessingEventSectionPage : EditSectionPage<SupportRequestProcessingEventInfo>
{
    protected override Task<string> GetObjectDisplayName(BaseInfo infoObject) =>
        infoObject is SupportRequestProcessingEventInfo eventInfo
            ? Task.FromResult($"Event {eventInfo.SupportRequestProcessingEventID}")
            : Task.FromResult("Event");
}
