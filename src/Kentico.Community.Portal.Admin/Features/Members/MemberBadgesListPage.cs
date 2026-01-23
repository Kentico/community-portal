using CMS.Base;
using CMS.Core;
using Kentico.Community.Portal.Admin.Features.MemberBadges;
using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: UIPage(
    uiPageType: typeof(MemberBadgesListPage),
    parentType: typeof(MemberEditSection),
    name: "Badges",
    slug: "badges",
    templateName: TemplateNames.LISTING,
    order: 1004
)]

namespace Kentico.Community.Portal.Admin.Features.Members;

public class MemberBadgesListPage(
    IConversionService conversionService,
    IPageLinkGenerator pageLinkGenerator) : ListingPage
{
    private readonly IConversionService conversionService = conversionService;
    private readonly IPageLinkGenerator pageLinkGenerator = pageLinkGenerator;

    [PageParameter(typeof(IntPageModelBinder), typeof(MemberEditSection))]
    public int ObjectId { get; set; }

    protected override string ObjectType => MemberBadgeInfo.OBJECT_TYPE;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        _ = PageConfiguration.HeaderActions.AddLink<MemberBadgeAssignmentEditPage>("Manage assigned badges", Icons.Edit, parameters: new PageParameterValues
        {
           { typeof(MemberBadgeAssignSectionPage), ObjectId }
        });

        _ = PageConfiguration.QueryModifiers
            .AddModifier((query, settings) =>
                query
                    .Source(source => source
                        .Join<MemberBadgeMemberInfo>(nameof(MemberBadgeInfo.MemberBadgeID), nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberBadgeId)))
                    .Where(w => w.WhereEquals(nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberId), ObjectId))
                    .OrderByDescending(nameof(MemberBadgeMemberInfo.MemberBadgeMemberCreatedDate), nameof(MemberBadgeMemberInfo.MemberBadgeMemberLastModified)));

        _ = PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(MemberBadgeInfo.MemberBadgeID),
                "Badge",
                searchable: true,
                minWidth: 1)
            .AddColumn(
                nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberBadgeId),
                "Member Badge",
                searchable: true,
                minWidth: 7)
            .AddColumn(
                nameof(MemberBadgeMemberInfo.MemberBadgeMemberCreatedDate),
                "Created",
                searchable: false,
                sortable: true,
                defaultSortDirection: SortTypeEnum.Desc)
            .AddColumn(
                nameof(MemberBadgeMemberInfo.MemberBadgeMemberLastModified),
                "Modified",
                searchable: true,
                sortable: true)
            .AddComponentColumn(nameof(MemberBadgeInfo.MemberBadgeDisplayName),
                "@kentico-community/portal-web-admin/Link",
                modelRetriever: MemberBadgeLinkModelRetriever,
                caption: "Badge",
                searchable: true,
                minWidth: 50);
    }

    private TableRowLinkProps MemberBadgeLinkModelRetriever(object value, IDataContainer container)
    {
        int badgeID = conversionService.GetInteger(container[nameof(MemberBadgeInfo.MemberBadgeID)], 0);
        string valueStr = value.ToString() ?? "";
        string label = valueStr.Length > 47
            ? $"{valueStr[..Math.Min(valueStr.Length, 47)]}..."
            : valueStr;

        if (badgeID == 0)
        {
            return new TableRowLinkProps() { Label = label, Path = "" };
        }

        string pageUrl = pageLinkGenerator.GetPath<MemberBadgeEditPage>(new()
        {
            { typeof(MemberBadgeSectionPage), badgeID },
        });

        return new TableRowLinkProps()
        {
            Label = label,
            Path = pageUrl.StartsWith('/')
                ? pageUrl[1..]
                : pageUrl
        };
    }
}
