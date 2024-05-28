using Kentico.Community.Portal.Admin.Features.SupportRequests;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(SupportRequestProcessingEventsApplicationPage),
    slug: "list",
    uiPageType: typeof(SupportRequestProcessingEventListingPage),
    name: "List",
    templateName: TemplateNames.LISTING,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.SupportRequests;

public class SupportRequestProcessingEventListingPage : ListingPage
{
    protected override string ObjectType => SupportRequestProcessingEventInfo.OBJECT_TYPE;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.AddEditRowAction<SupportRequestProcessingEventEditPage>();

        PageConfiguration.TableActions.AddCommand("Requeue", nameof(Requeue), Icons.ArrowCurvedRight);

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(SupportRequestProcessingEventInfo.SupportRequestProcessingEventID),
                "ID",
                searchable: true,
                maxWidth: 1)
            .AddColumn(nameof(SupportRequestProcessingEventInfo.SupportRequestProcessingEventMessageID),
                "Message ID",
                searchable: true,
                maxWidth: 5)
            .AddColumn(nameof(SupportRequestProcessingEventInfo.SupportRequestProcessingEventMessage),
                "Message",
                searchable: true,
                minWidth: 30)
            .AddColumn(nameof(SupportRequestProcessingEventInfo.SupportRequestProcessingEventStatus),
                "Status",
                searchable: true,
                maxWidth: 2)
            .AddColumn(
                nameof(SupportRequestProcessingEventInfo.SupportRequestProcessingEventLastModified),
                "Last Modified",
                searchable: true,
                maxWidth: 5);
    }

    [PageCommand]
    public async Task<ICommandResponse<RowActionResult>> Requeue(int eventID)
    {
        await Task.Delay(2);

        return new CommandResponse<RowActionResult>(new RowActionResult(true, true))
        {
            CommandName = nameof(Requeue),
        };
    }
}

