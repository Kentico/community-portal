using CMS.Base;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.MemberBadges;
using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(MemberBadgeApplicationPage),
    slug: "assignments",
    uiPageType: typeof(MemberBadgeAssignmentListingPage),
    name: "Assign Badges List",
    templateName: TemplateNames.LISTING,
    order: 10)]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

[UIEvaluatePermission(SystemPermissions.VIEW)]
internal class MemberBadgeAssignmentListingPage : ListingPage
{
    protected override string ObjectType => MemberInfo.OBJECT_TYPE;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.HeaderActions.AddLink<MemberBadgeListingPage>("Badge List");
        PageConfiguration.AddEditRowAction<MemberBadgeAssignmentEditPage>();

        PageConfiguration.QueryModifiers
            .AddModifier((q, settings) =>
            {
                MemberListFilter.ModifyQueryForBadgeFilter(q);

                var ruleAssigned =
                    new ObjectQuery(MemberBadgeMemberInfo.OBJECT_TYPE)
                        .Source(s => s.Join<MemberBadgeInfo>(nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberBadgeId), nameof(MemberBadgeInfo.MemberBadgeID)))
                        .Column(new AggregatedColumn(AggregationType.Count, nameof(MemberBadgeMemberInfo.MemberBadgeMemberID)))
                        .Where($"{nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberId)} = {nameof(MemberInfo.MemberID)} AND {nameof(MemberBadgeInfo.MemberBadgeIsRuleAssigned)} = 1");

                var manuallyAssigned =
                    new ObjectQuery(MemberBadgeMemberInfo.OBJECT_TYPE)
                        .Source(s => s.Join<MemberBadgeInfo>(nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberBadgeId), nameof(MemberBadgeInfo.MemberBadgeID)))
                        .Column(new AggregatedColumn(AggregationType.Count, nameof(MemberBadgeMemberInfo.MemberBadgeMemberID)))
                        .Where($"{nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberId)} = {nameof(MemberInfo.MemberID)} AND {nameof(MemberBadgeInfo.MemberBadgeIsRuleAssigned)} = 0");

                return q
                    .Columns(
                        // We have to specify an explicit column list because .AddColumn() won't expand the aggregate into a subquery
                        new QueryColumn(nameof(MemberInfo.MemberID)),
                        new QueryColumn(nameof(MemberInfo.MemberName)),
                        new QueryColumn(nameof(MemberInfo.MemberEmail)),
                        new QueryColumn(nameof(MemberInfo.MemberEnabled)),
                        ruleAssigned.AsColumn("MemberBadgesRuleAssignedCount"),
                        manuallyAssigned.AsColumn("MemberBadgesManuallyAssignedCount"));
            });

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(MemberInfo.MemberID),
                "ID",
                searchable: true,
                minWidth: 1,
                defaultSortDirection: SortTypeEnum.Desc)
            .AddColumn(nameof(MemberInfo.MemberName), "Username",
                searchable: true)
            .AddColumn(nameof(MemberInfo.MemberEmail), "Email",
                searchable: true)
            .AddComponentColumn(
                    nameof(MemberInfo.MemberEnabled),
                    NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT,
                    LocalizationService.GetString("base.members.list.state"),
                    modelRetriever: GetStateCellModel,
                    sortable: false)
            .AddColumn("MemberBadgesRuleAssignedCount", "Rule Assigned Badges")
            .AddColumn("MemberBadgesManuallyAssignedCount", "Manually Assigned Badges");

        PageConfiguration.FilterFormModel = new MemberListFilter();
    }

    private SimpleStatusNamedComponentCellProps GetStateCellModel(object isEnabled, IDataContainer dataContainer) =>
        (bool)isEnabled
            ? ActiveStatusCellProps()
            : InactiveStatusCellProps();


    /// <summary>
    /// Represents an enabled member. Returned by <see cref="GetStateCellModel(object, IDataContainer)"/>.
    /// </summary>
    private SimpleStatusNamedComponentCellProps ActiveStatusCellProps() => new()
    {
        Label = LocalizationService.GetString("base.members.status.active"),
        LabelColor = Color.SuccessText,
        IconName = Icons.CheckCircle,
        IconColor = Color.SuccessIcon
    };


    /// <summary>
    /// Represents a disabled member. Returned by <see cref="GetStateCellModel(object, IDataContainer)"/>.
    /// </summary>
    private SimpleStatusNamedComponentCellProps InactiveStatusCellProps() => new()
    {
        Label = LocalizationService.GetString("base.members.status.inactive"),
        LabelColor = Color.TextLowEmphasis,
        IconName = Icons.MinusCircle,
        IconColor = Color.IconDefault
    };
}


