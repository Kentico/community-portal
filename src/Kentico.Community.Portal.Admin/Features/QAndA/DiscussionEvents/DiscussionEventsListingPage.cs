using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Admin.Features.QAndA.DiscussionEvents;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using CMS.Websites.Internal;

[assembly: UIPage(
    parentType: typeof(QAndAApplicationPage),
    slug: "discussion-events",
    uiPageType: typeof(DiscussionEventsListingPage),
    name: "Discussion events",
    templateName: TemplateNames.LISTING,
    order: 1)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.DiscussionEvents;

public class DiscussionEventsListingPage : ListingPage
{
    protected override string ObjectType => DiscussionEventInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.HeaderActions.AddLink<DiscussionEventsCreatePage>("Create event");

        PageConfiguration.AddEditRowAction<DiscussionEventsEditPage>();

        PageConfiguration.QueryModifiers
            .AddModifier((query, settings) =>
                query.Source(source =>
                    source
                    .Join<MemberInfo>(
                        nameof(DiscussionEventInfo.DiscussionEventFromMemberID),
                        nameof(MemberInfo.MemberID))
                    .Join<WebPageItemInfo>(
                        $"KenticoCommunity_DiscussionEvent.{nameof(DiscussionEventInfo.DiscussionEventWebPageItemID)}",
                        nameof(WebPageItemInfo.WebPageItemID))));


        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(DiscussionEventInfo.DiscussionEventID),
                "ID",
                searchable: true,
                minWidth: 1)
            .AddColumn(
                nameof(MemberInfo.MemberEmail),
                "From member",
                searchable: true,
                minWidth: 7)
            .AddColumn(
                nameof(WebPageItemInfo.WebPageItemName),
                "Question",
                searchable: true,
                minWidth: 7)
            .AddColumn(
                nameof(DiscussionEventInfo.DiscussionEventDateModified),
                "Date",
                searchable: false,
                sortable: true,
                minWidth: 4)
            .AddColumn(
                nameof(DiscussionEventInfo.DiscussionEventType),
                "Type",
                searchable: true);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
