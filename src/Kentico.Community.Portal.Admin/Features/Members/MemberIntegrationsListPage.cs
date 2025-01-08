using CMS.Base;
using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Content;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: UIPage(
    uiPageType: typeof(MemberIntegrationsListPage),
    parentType: typeof(MemberEditSection),
    name: "Integrations",
    slug: "integrations",
    templateName: TemplateNames.LISTING,
    order: 1003
)]

namespace Kentico.Community.Portal.Admin.Features.Members;

public class MemberIntegrationsListPage(
    IConversionService conversionService,
    IPageLinkGenerator pageLinkGenerator) : ListingPage
{
    private readonly IConversionService conversionService = conversionService;
    private readonly IPageLinkGenerator pageLinkGenerator = pageLinkGenerator;

    [PageParameter(typeof(IntPageModelBinder), typeof(MemberEditSection))]
    public int ObjectId { get; set; }

    protected override string ObjectType => ContentItemInfo.OBJECT_TYPE;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.QueryModifiers
            .AddModifier((query, settings) =>
            {
                return query
                    .Source(source => source
                        .Join(new QuerySourceTable("CMS_ContentItemCommonData"),
                            "CMS_ContentItem.ContentItemID",
                            "CMS_ContentItemCommonData.ContentItemCommonDataContentItemID")
                        .Join(new QuerySourceTable("KenticoCommunity_IntegrationContent"),
                            "CMS_ContentItemCommonData.ContentItemCommonDataID",
                            "KenticoCommunity_IntegrationContent.ContentItemDataCommonDataID"))
                    .Where(w => w.WhereEquals(nameof(IntegrationContent.IntegrationContentAuthorMemberID), ObjectId))
                    .OrderByDescending(nameof(IntegrationContent.IntegrationContentPublishedDate));
            });

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(IntegrationContent.SystemFields.ContentItemID),
                "Content Item",
                searchable: true,
                minWidth: 1)
            .AddColumn(nameof(ContentItemInfo.ContentItemWorkspaceID),
                nameof(ContentItemInfo.ContentItemWorkspaceID),
                visible: false)
            .AddColumn(
                nameof(IntegrationContent.IntegrationContentPublishedDate),
                "Published",
                searchable: false,
                sortable: true,
                defaultSortDirection: SortTypeEnum.Desc)
            .AddComponentColumn(nameof(IntegrationContent.ListableItemTitle),
                "@kentico-community/portal-web-admin/Link",
                modelRetriever: ContentLinkModelRetriever,
                caption: "Integration",
                searchable: true,
                minWidth: 50);
    }

    private TableRowLinkProps ContentLinkModelRetriever(object value, IDataContainer container)
    {
        int contentItemID = conversionService.GetInteger(container[nameof(IntegrationContent.SystemFields.ContentItemID)], 0);
        int workspaceID = conversionService.GetInteger(container[nameof(ContentItemInfo.ContentItemWorkspaceID)], 0);
        string valueStr = value.ToString() ?? "";
        string label = valueStr.Length > 47
            ? $"{valueStr[..Math.Min(valueStr.Length, 47)]}..."
            : valueStr;

        if (contentItemID == 0)
        {
            return new TableRowLinkProps() { Label = label, Path = "" };
        }

        string pageUrl = pageLinkGenerator.GetPath<ContentItemEdit>(new()
        {
            { typeof(ContentHubWorkspace), workspaceID },
            { typeof(ContentHubContentLanguage), PortalWebSiteChannel.DEFAULT_LANGUAGE },
            { typeof(ContentItemEditSection), contentItemID },
            { typeof(ContentHubFolder), ContentHubSlugs.ALL_CONTENT_ITEMS },
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
