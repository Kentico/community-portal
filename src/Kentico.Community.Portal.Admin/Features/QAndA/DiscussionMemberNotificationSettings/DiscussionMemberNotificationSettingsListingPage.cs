using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberNotificationSettings;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(QAndAApplicationPage),
    slug: "member-notification-settings",
    uiPageType: typeof(DiscussionMemberNotificationSettingsListingPage),
    name: "Member notification settings",
    templateName: TemplateNames.LISTING,
    order: 2)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.DiscussionMemberNotificationSettings;

public class DiscussionMemberNotificationSettingsListingPage : ListingPage
{
    protected override string ObjectType => DiscussionMemberNotificationSettingsInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.HeaderActions.AddLink<DiscussionMemberNotificationSettingsCreatePage>("Create settings");

        PageConfiguration.AddEditRowAction<DiscussionMemberNotificationSettingsEditPage>();

        PageConfiguration.QueryModifiers
            .AddModifier((query, settings) => query.Source(source =>
                source.Join<MemberInfo>(
                    nameof(DiscussionMemberNotificationSettingsInfo.DiscussionMemberNotificationSettingsMemberID),
                    nameof(MemberInfo.MemberID))));


        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(DiscussionMemberNotificationSettingsInfo.DiscussionMemberNotificationSettingsID),
                "ID",
                searchable: true,
                minWidth: 1)
            .AddColumn(
                nameof(MemberInfo.MemberEmail),
                "Member",
                searchable: true,
                minWidth: 7)
             .AddColumn(
                nameof(DiscussionMemberNotificationSettingsInfo.DiscussionMemberNotificationSettingsDateModified),
                "Modified",
                searchable: false,
                sortable: true,
                minWidth: 4)
            .AddColumn(
                nameof(DiscussionMemberNotificationSettingsInfo.DiscussionMemberNotificationSettingsFrequencyType),
                "Frequency",
                searchable: true);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
