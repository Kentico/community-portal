using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Kentico.Xperience.Admin.Websites;
using Kentico.Xperience.Admin.Websites.UIPages;

[assembly: UIPage(
    uiPageType: typeof(MemberQAndAAnswersListPage),
    parentType: typeof(MemberEditSection),
    name: "Answers",
    slug: "answers",
    templateName: TemplateNames.LISTING,
    order: 1001
)]

namespace Kentico.Community.Portal.Admin.Features.Members;

public class MemberQAndAAnswersListPage(
    IConversionService conversionService,
    IPageLinkGenerator pageLinkGenerator) : ListingPage
{
    private readonly IConversionService conversionService = conversionService;
    private readonly IPageLinkGenerator pageLinkGenerator = pageLinkGenerator;

    [PageParameter(typeof(IntPageModelBinder), typeof(MemberEditSection))]
    public int ObjectId { get; set; }

    protected override string ObjectType => QAndAAnswerDataInfo.OBJECT_TYPE;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        _ = PageConfiguration.QueryModifiers
            .AddModifier((query, settings) =>
                query
                    .Source(source => source
                        .Join<WebPageItemInfo>($"KenticoCommunity_QAndAAnswerData.{nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID)}", nameof(WebPageItemInfo.WebPageItemID))
                        .Join(new QuerySourceTable("CMS_ContentItemCommonData"),
                            "CMS_WebPageItem.WebPageItemContentItemID",
                            "CMS_ContentItemCommonData.ContentItemCommonDataContentItemID")
                        .Join(new QuerySourceTable("KenticoCommunity_QAndAQuestionPage"),
                            "CMS_ContentItemCommonData.ContentItemCommonDataID",
                            "KenticoCommunity_QAndAQuestionPage.ContentItemDataCommonDataID"))
                    .Where(w => w.WhereEquals(nameof(QAndAAnswerDataInfo.QAndAAnswerDataAuthorMemberID), ObjectId))
                    .OrderByDescending(nameof(QAndAAnswerDataInfo.QAndAAnswerDataDateCreated), nameof(QAndAAnswerDataInfo.QAndAAnswerDataDateModified)));

        _ = PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(QAndAAnswerDataInfo.QAndAAnswerDataID),
                "Answer ID",
                searchable: true,
                minWidth: 1)
            .AddColumn(nameof(QAndAQuestionPage.QAndAQuestionPageAcceptedAnswerDataGUID), visible: false)
            .AddComponentColumn(nameof(QAndAAnswerDataInfo.QAndAAnswerDataGUID),
                NamedComponentCellComponentNames.SIMPLE_STATUS_COMPONENT,
                "Accepted?",
                modelRetriever: AcceptAnswerModelRetriever,
                searchable: false,
                sortable: false,
                minWidth: 1)
            .AddColumn(
                nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID),
                "Web Page ID",
                searchable: true,
                minWidth: 7)
            .AddColumn(
                nameof(QAndAAnswerDataInfo.QAndAAnswerDataDateCreated),
                "Created",
                searchable: false,
                sortable: true,
                defaultSortDirection: SortTypeEnum.Desc)
            .AddColumn(
                nameof(QAndAAnswerDataInfo.QAndAAnswerDataDateModified),
                "Modified",
                searchable: true,
                sortable: true)
            .AddColumn(nameof(QAndAQuestionPage.SystemFields.WebPageItemWebsiteChannelId), visible: false)
            .AddColumn(nameof(QAndAQuestionPage.SystemFields.WebPageItemID), visible: false)
            .AddComponentColumn(nameof(QAndAAnswerDataInfo.QAndAAnswerDataCodeName),
                "@kentico-community/portal-web-admin/Link",
                modelRetriever: AnswerLinkModelRetriever,
                caption: "Answer",
                searchable: true,
                minWidth: 50)
            .AddComponentColumn(nameof(WebPageItemInfo.WebPageItemTreePath),
                "@kentico-community/portal-web-admin/Link",
                modelRetriever: QuestionLinkModelRetriever,
                caption: "Question",
                searchable: true,
                minWidth: 50);
    }

    private SimpleStatusNamedComponentCellProps AcceptAnswerModelRetriever(object value, IDataContainer container)
    {
        var acceptedAnswerGUID = conversionService.GetGuid(container[nameof(QAndAQuestionPage.QAndAQuestionPageAcceptedAnswerDataGUID)], default);
        var answerGUID = conversionService.GetGuid(value, default);

        return acceptedAnswerGUID == answerGUID && acceptedAnswerGUID != default
            ? new SimpleStatusNamedComponentCellProps()
            {
                Label = "Accepted",
                LabelColor = Color.SuccessText,
                IconName = Icons.CheckCircle,
                IconColor = Color.SuccessIcon
            }
            : new SimpleStatusNamedComponentCellProps();
    }

    private TableRowLinkProps AnswerLinkModelRetriever(object value, IDataContainer container)
    {
        int answerID = conversionService.GetInteger(container[nameof(QAndAAnswerDataInfo.QAndAAnswerDataID)], 0);
        string valueStr = value.ToString() ?? "";
        string label = valueStr.Length > 47
            ? $"{valueStr[..Math.Min(valueStr.Length, 47)]}..."
            : valueStr;

        if (answerID == 0)
        {
            return new TableRowLinkProps() { Label = label, Path = "" };
        }

        string pageUrl = pageLinkGenerator.GetPath<QAndAEditPage>(new()
        {
            { typeof(QAndASectionPage), answerID },
        });

        return new TableRowLinkProps()
        {
            Label = label,
            Path = pageUrl.StartsWith('/')
                ? pageUrl[1..]
                : pageUrl
        };
    }

    private TableRowLinkProps QuestionLinkModelRetriever(object value, IDataContainer container)
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
            { typeof(WebPageLayout), new WebPageUrlIdentifier(PortalWebSiteChannel.DEFAULT_LANGUAGE, webPageItemID) },
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
