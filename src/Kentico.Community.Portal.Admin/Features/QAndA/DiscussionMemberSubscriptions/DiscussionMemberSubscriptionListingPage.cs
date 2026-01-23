using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberSubscriptions;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using CMS.Websites.Internal;

[assembly: UIPage(
    parentType: typeof(QAndAApplicationPage),
    slug: "member-subscriptions",
    uiPageType: typeof(DiscussionMemberSubscriptionListingPage),
    name: "Member subscriptions",
    templateName: TemplateNames.LISTING,
    order: 3)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberSubscriptions;

public class DiscussionMemberSubscriptionListingPage : ListingPage
{
    protected override string ObjectType => DiscussionMemberSubscriptionInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.HeaderActions.AddLink<DiscussionMemberSubscriptionCreatePage>("Create subscription");

        PageConfiguration.AddEditRowAction<DiscussionMemberSubscriptionEditPage>();

        PageConfiguration.QueryModifiers
            .AddModifier((query, settings) => query.Source(source =>
                source
                    .Join<MemberInfo>(
                        nameof(DiscussionMemberSubscriptionInfo.DiscussionMemberSubscriptionMemberID),
                        nameof(MemberInfo.MemberID))
                    .Join<WebPageItemInfo>(
                        $"KenticoCommunity_DiscussionMemberSubscription.{nameof(DiscussionMemberSubscriptionInfo.DiscussionMemberSubscriptionWebPageItemID)}",
                        nameof(WebPageItemInfo.WebPageItemID))));

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(DiscussionMemberSubscriptionInfo.DiscussionMemberSubscriptionID),
                "ID",
                searchable: true,
                minWidth: 1)
            .AddColumn(
                nameof(MemberInfo.MemberEmail),
                "Member",
                searchable: true,
                minWidth: 7)
            .AddColumn(
                nameof(WebPageItemInfo.WebPageItemName),
                "Question",
                searchable: true,
                minWidth: 7);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
