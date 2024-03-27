using CMS.Base;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;
using CMS.Websites;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(QAndAApplicationPage),
    slug: "list",
    uiPageType: typeof(QAndAListingPage),
    name: "Answers",
    templateName: TemplateNames.LISTING,
    order: 0)]

namespace Kentico.Community.Portal.Admin.Features.QAndA;

public class QAndAListingPage(IInfoProvider<WebsiteChannelInfo> channelProvider) : ListingPage
{
    private readonly IInfoProvider<WebsiteChannelInfo> channelProvider = channelProvider;

    protected override string ObjectType => QAndAAnswerDataInfo.OBJECT_TYPE;

    /// <summary>
    /// Deletes user specified by the <paramref name="id"/> parameter.
    /// </summary>
    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        int websiteChannelID = (await channelProvider.Get()
            .Source(s => s.Join<ChannelInfo>(nameof(WebsiteChannelInfo.WebsiteChannelChannelID), nameof(ChannelInfo.ChannelID)))
            .WhereEquals(nameof(ChannelInfo.ChannelName), PortalWebSiteChannel.CODE_NAME)
            .Columns(nameof(WebsiteChannelInfo.WebsiteChannelID))
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault()?
            .WebsiteChannelID ?? 0;

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
                searchable: true)
            .AddColumn(
                nameof(QAndAAnswerDataInfo.QAndAAnswerDataDateModified),
                "Modified",
                searchable: true)
            .AddColumn(nameof(QAndAAnswerDataInfo.QAndAAnswerDataCodeName),
                "Answer CodeName",
                searchable: true)
            .AddComponentColumn(nameof(WebPageItemInfo.WebPageItemName),
                "@kentico-community/portal-web-admin/Link",
                modelRetriever: ModelRetriever(websiteChannelID),
                caption: "Question",
                searchable: true,
                minWidth: 25);

        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }

    private static Func<object, IDataContainer, object> ModelRetriever(int websiteChannelID) =>
        (object value, IDataContainer container) =>
        {
            int webPageItemID = ValidationHelper.GetInteger(container[nameof(WebPageItemInfo.WebPageItemID)], 0);
            string valueStr = value.ToString() ?? "";
            string label = $"{valueStr[..Math.Min(valueStr.Length, 50)]}...";

            if (webPageItemID == 0)
            {
                return new TableRowLinkProps() { Label = label, Path = "" };
            }

            return new TableRowLinkProps()
            {
                Label = label,
                Path = $"/admin/webpages-{websiteChannelID}/en-US_{webPageItemID}/content"
            };
        };

}

public class TableRowLinkProps
{
    public string Path { get; set; } = "";
    public string Label { get; set; } = "";
}
