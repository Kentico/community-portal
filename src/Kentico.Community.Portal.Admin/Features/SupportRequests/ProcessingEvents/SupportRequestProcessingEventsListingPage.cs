using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(SupportRequestProcessingEventsApplicationPage),
    slug: "list",
    uiPageType: typeof(SupportRequestProcessingEventsListingPage),
    name: "List",
    templateName: TemplateNames.LISTING,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestProcessingEventsListingPage : ListingPage
{
    protected override string ObjectType => SupportRequestProcessingEventInfo.OBJECT_TYPE;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(SupportRequestProcessingEventInfo.SupportRequestProcessingEventID),
                "ID",
                searchable: true)
            .AddColumn(nameof(SupportRequestProcessingEventInfo.SupportRequestProcessingEventMessageID),
                "Message ID",
                searchable: true)
            .AddColumn(nameof(SupportRequestProcessingEventInfo.SupportRequestProcessingEventMessage),
                "Message",
                searchable: true)
            .AddColumn(nameof(SupportRequestProcessingEventInfo.SupportRequestProcessingEventStatus),
                "Status",
                searchable: true)
            .AddColumn(
                nameof(SupportRequestProcessingEventInfo.SupportRequestProcessingEventLastModified),
                "Last Modified",
                searchable: true);
    }
}

