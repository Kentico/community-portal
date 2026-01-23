using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.QAndA.DiscussionEvents;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(DiscussionEventsListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(DiscussionEventsSectionPage),
    name: "Discussion Event Section",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.DiscussionEvents;

public class DiscussionEventsSectionPage : EditSectionPage<DiscussionEventInfo>
{
    protected override Task<string> GetObjectDisplayName(BaseInfo infoObject) =>
        infoObject is DiscussionEventInfo eventInfo
            ? Task.FromResult($"Event {eventInfo.DiscussionEventID}")
            : Task.FromResult("Event");
}
