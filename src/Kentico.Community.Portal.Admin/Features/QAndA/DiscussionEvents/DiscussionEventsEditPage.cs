using Kentico.Community.Portal.Admin.Features.QAndA.DiscussionEvents;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(DiscussionEventsSectionPage),
    slug: "edit",
    uiPageType: typeof(DiscussionEventsEditPage),
    name: "Edit Discussion Event",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.DiscussionEvents;

public class DiscussionEventsEditPage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder)
    : InfoEditPage<DiscussionEventInfo>(formComponentMapper, formDataBinder)
{
    [PageParameter(typeof(IntPageModelBinder))]
    public override int ObjectId { get; set; }

    protected override Task<string> GetObjectDisplayName(DiscussionEventInfo infoObject) =>
        infoObject is DiscussionEventInfo eventInfo
            ? Task.FromResult($"Event {eventInfo.DiscussionEventID}")
            : Task.FromResult("Event");
}
