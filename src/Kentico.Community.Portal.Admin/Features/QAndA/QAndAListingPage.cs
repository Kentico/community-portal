using CMS.Base;
using CMS.Helpers;
using CMS.Membership;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Websites.UIPages;

[assembly: UIPage(
    parentType: typeof(QAndAApplicationPage),
    slug: "answers",
    uiPageType: typeof(QAndAListingPage),
    name: "Answers List",
    templateName: TemplateNames.LISTING,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.QAndA;

public class QAndAListingPage(IPageLinkGenerator pageLinkGenerator) : ListingPage
{
    private readonly IPageLinkGenerator pageLinkGenerator = pageLinkGenerator;

    protected override string ObjectType => QAndAAnswerDataInfo.OBJECT_TYPE;

    /// <summary>
    /// Deletes user specified by the <paramref name="id"/> parameter.
    /// </summary>
    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.HeaderActions.AddLink<QAndACreatePage>("Create Answer");

        PageConfiguration.AddEditRowAction<QAndAEditPage>();

        PageConfiguration.QueryModifiers
            .AddModifier((query, settings) =>
            {
                return query
                    .Source(source => source
                        .Join<MemberInfo>(nameof(QAndAAnswerDataInfo.QAndAAnswerDataAuthorMemberID), nameof(MemberInfo.MemberID))
                        .Join<WebPageItemInfo>($"KenticoCommunity_QAndAAnswerData.{nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID)}", nameof(WebPageItemInfo.WebPageItemID)))
                   .OrderByDescending(nameof(QAndAAnswerDataInfo.QAndAAnswerDataDateCreated), nameof(QAndAAnswerDataInfo.QAndAAnswerDataDateModified));
            });

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(QAndAAnswerDataInfo.QAndAAnswerDataID),
                "ID",
                searchable: true,
                minWidth: 1)
            .AddColumn(
                nameof(WebPageItemInfo.WebPageItemID),
                "Web Page",
                searchable: true,
                minWidth: 7)
            .AddColumn(
                nameof(MemberInfo.MemberEmail),
                "Author",
                searchable: true)
            .AddColumn(
                nameof(QAndAAnswerDataInfo.QAndAAnswerDataDateCreated),
                "Created",
                searchable: true,
                sortable: true,
                defaultSortDirection: SortTypeEnum.Desc)
            .AddColumn(
                nameof(QAndAAnswerDataInfo.QAndAAnswerDataDateModified),
                "Modified",
                searchable: true,
                sortable: true)
            .AddColumn(nameof(QAndAAnswerDataInfo.QAndAAnswerDataCodeName),
                "Answer CodeName",
                searchable: true)
            .AddColumn(nameof(QAndAAnswerDataInfo.QAndAAnswerDataWebsiteChannelID), visible: false)
            .AddComponentColumn(nameof(WebPageItemInfo.WebPageItemName),
                "@kentico-community/portal-web-admin/Link",
                modelRetriever: AnswerLinkModelRetriever,
                caption: "Question",
                searchable: true,
                minWidth: 25);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }

    private TableRowLinkProps AnswerLinkModelRetriever(object value, IDataContainer container)
    {
        int webPageItemID = ValidationHelper.GetInteger(container[nameof(WebPageItemInfo.WebPageItemID)], 0);
        int websiteChannelID = ValidationHelper.GetInteger(container[nameof(QAndAAnswerDataInfo.QAndAAnswerDataWebsiteChannelID)], 0);
        string valueStr = value.ToString() ?? "";
        string label = $"{valueStr[..Math.Min(valueStr.Length, 50)]}...";

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

public class TableRowLinkProps
{
    public string Path { get; set; } = "";
    public string Label { get; set; } = "";
}
