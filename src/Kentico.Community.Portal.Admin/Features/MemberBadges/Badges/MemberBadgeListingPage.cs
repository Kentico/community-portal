using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.MemberBadges;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(MemberBadgeApplicationPage),
    slug: "badges",
    uiPageType: typeof(MemberBadgeListingPage),
    name: "Badge List",
    templateName: TemplateNames.LISTING,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

[UIEvaluatePermission(SystemPermissions.VIEW)]
public class MemberBadgeListingPage(
    IPageLinkGenerator pageLinkGenerator,
    IInfoProvider<MemberBadgeMemberInfo> memberBadgeMemberProvider,
    IInfoProvider<MemberBadgeInfo> memberBadgeProvider) : ListingPage
{
    private readonly IPageLinkGenerator pageLinkGenerator = pageLinkGenerator;
    private readonly IInfoProvider<MemberBadgeMemberInfo> memberBadgeMemberProvider = memberBadgeMemberProvider;
    private readonly IInfoProvider<MemberBadgeInfo> memberBadgeProvider = memberBadgeProvider;

    protected override string ObjectType => MemberBadgeInfo.OBJECT_TYPE;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.HeaderActions.AddLink<MemberBadgeCreatePage>("Create Badge");
        PageConfiguration.AddEditRowAction<MemberBadgeEditPage>();
        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(MemberBadgeInfo.MemberBadgeID),
                "ID",
                maxWidth: 5)
            .AddColumn(nameof(MemberBadgeInfo.MemberBadgeDisplayName),
                "Display Name",
                searchable: true,
                maxWidth: 75)
            .AddColumn(nameof(MemberBadgeInfo.MemberBadgeCodeName),
                "Code Name",
                searchable: true,
                maxWidth: 75)
            .AddColumn(nameof(MemberBadgeInfo.MemberBadgeShortDescription),
                "Description",
                searchable: true,
                minWidth: 110)
            .AddColumn(nameof(MemberBadgeInfo.MemberBadgeDateCreated),
                "Created",
                sortable: true,
                maxWidth: 5,
                defaultSortDirection: SortTypeEnum.Desc);
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public async Task<ICommandResponse> Delete(int id, CancellationToken cancellationToken)
    {
        memberBadgeMemberProvider.BulkDelete(new WhereCondition($"{nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberBadgeId)} = {id}"));
        var badge = await memberBadgeProvider.GetAsync(id, cancellationToken);

        if (badge is not null)
        {
            _ = badge.Delete();
        }

        var response = NavigateTo(pageLinkGenerator.GetPath<MemberBadgeListingPage>());

        return response;
    }
}
