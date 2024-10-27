using CMS.Base;
using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Content;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Kentico.Xperience.Admin.Websites.UIPages;

[assembly: UIPage(
    uiPageType: typeof(MemberQAndAQuestionsListPage),
    parentType: typeof(MemberEditSection),
    name: "Questions",
    slug: "questions",
    templateName: TemplateNames.LISTING,
    order: 1000
)]

namespace Kentico.Community.Portal.Admin.Features.Members;

public class MemberQAndAQuestionsListPage(
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
                        .Join<WebPageItemInfo>(nameof(ContentItemInfo.ContentItemID), nameof(WebPageItemInfo.WebPageItemContentItemID))
                        .Join(new QuerySourceTable("CMS_ContentItemCommonData"),
                            "CMS_ContentItem.ContentItemID",
                            "CMS_ContentItemCommonData.ContentItemCommonDataContentItemID")
                        .Join(new QuerySourceTable("KenticoCommunity_QAndAQuestionPage"),
                            "CMS_ContentItemCommonData.ContentItemCommonDataID",
                            "KenticoCommunity_QAndAQuestionPage.ContentItemDataCommonDataID"))
                    .Where(w => w.WhereEquals(nameof(QAndAQuestionPage.QAndAQuestionPageAuthorMemberID), ObjectId))
                    .OrderByDescending(nameof(QAndAQuestionPage.QAndAQuestionPageDateCreated), nameof(QAndAQuestionPage.QAndAQuestionPageDateModified));
            });

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(QAndAQuestionPage.SystemFields.ContentItemID),
                "Content Item",
                searchable: true,
                minWidth: 1)
            .AddColumn(
                nameof(QAndAQuestionPage.SystemFields.WebPageItemID),
                "Web Page",
                searchable: true,
                minWidth: 7)
            .AddColumn(
                nameof(QAndAQuestionPage.QAndAQuestionPageDateCreated),
                "Created",
                searchable: false,
                sortable: true,
                defaultSortDirection: SortTypeEnum.Desc)
            .AddColumn(
                nameof(QAndAQuestionPage.QAndAQuestionPageDateModified),
                "Modified",
                searchable: true,
                sortable: true)
            .AddColumn(nameof(QAndAQuestionPage.SystemFields.WebPageItemWebsiteChannelId), visible: false)
            .AddComponentColumn(nameof(WebPageItemInfo.WebPageItemTreePath),
                "@kentico-community/portal-web-admin/Link",
                modelRetriever: QuestionPageLinkModelRetriever,
                caption: "Question",
                searchable: true,
                minWidth: 50);
    }

    private TableRowLinkProps QuestionPageLinkModelRetriever(object value, IDataContainer container)
    {
        int webPageItemID = conversionService.GetInteger(container[nameof(QAndAQuestionPage.SystemFields.WebPageItemID)], 0);
        int websiteChannelID = conversionService.GetInteger(container[nameof(QAndAQuestionPage.SystemFields.WebPageItemWebsiteChannelId)], 0);
        string valueStr = value.ToString() ?? "";
        string label = valueStr.Length > 47
            ? $"{valueStr[..Math.Min(valueStr.Length, 47)]}..."
            : valueStr;

        if (webPageItemID == 0)
        {
            return new TableRowLinkProps() { Label = label, Path = "" };
        }

        string pageUrl = pageLinkGenerator.GetPath<ContentTab>(new()
        {
            { typeof(WebPageLayout), $"{PortalWebSiteChannel.DEFAULT_LANGUAGE}_{webPageItemID}" },
            { typeof(WebPagesApplication), $"webpages-{websiteChannelID}" },
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
