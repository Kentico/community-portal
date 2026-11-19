using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.PublishingCalendar;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    uiPageType: typeof(PublishingCalendarPage),
    parentType: typeof(PublishingCalendarApplicationPage),
    slug: "calendar",
    name: "Calendar",
    templateName: "@kentico-community/portal-web-admin/PublishingCalendar",
    order: 0,
    Icon = Icons.Calendar)]

namespace Kentico.Community.Portal.Admin.Features.PublishingCalendar;

[UIPermission(SystemPermissions.VIEW)]
public class PublishingCalendarPage(IPageLinkGenerator pageLinkGenerator) : Page<PublishingCalendarPageClientProperties>
{
    private readonly PublishingCalendarDataSource dataSource = new(pageLinkGenerator);

    public const string LOAD_CALENDAR_COMMAND = "LoadCalendar";
    public const string LOAD_DAY_ITEMS_COMMAND = "LoadDayItems";

    /// <summary>
    /// Items load on the client because day boundaries depend on the browser's time zone.
    /// </summary>
    public override async Task<PublishingCalendarPageClientProperties> ConfigureTemplateProperties(PublishingCalendarPageClientProperties properties)
    {
        properties.LoadCalendarCommandName = LOAD_CALENDAR_COMMAND;
        properties.LoadDayItemsCommandName = LOAD_DAY_ITEMS_COMMAND;
        properties.DayItemsPageSize = PublishingCalendarDataSource.DAY_ITEMS_PAGE_SIZE;
        properties.ContentTypes = await dataSource.LoadContentTypeOptions(CancellationToken.None);
        properties.Channels = await dataSource.LoadChannelOptions(CancellationToken.None);
        properties.Workflows = await dataSource.LoadWorkflowOptions(CancellationToken.None);

        return properties;
    }

    [PageCommand(CommandName = LOAD_CALENDAR_COMMAND)]
    public async Task<ICommandResponse> LoadCalendar(LoadCalendarParams commandParams, CancellationToken cancellationToken) =>
        ResponseFrom(await dataSource.LoadCalendar(commandParams, cancellationToken));

    [PageCommand(CommandName = LOAD_DAY_ITEMS_COMMAND)]
    public async Task<ICommandResponse> LoadDayItems(LoadDayItemsParams commandParams, CancellationToken cancellationToken) =>
        ResponseFrom(await dataSource.LoadDayItems(commandParams, cancellationToken));
}
