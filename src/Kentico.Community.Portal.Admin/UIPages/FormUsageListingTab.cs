using CMS.Base;
using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.DataEngine;
using CMS.Membership;
using CMS.OnlineForms;
using CMS.Websites;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;
using Kentico.Xperience.Admin.Websites.UIPages;

[assembly: UIPage(
    parentType: typeof(FormEditSection),
    slug: "usage",
    uiPageType: typeof(FormUsageListingTab),
    name: "Form usage",
    templateName: TemplateNames.LISTING,
    order: 1000,
    icon: Icons.MultiChannel)]

namespace Kentico.Community.Portal.Admin.UIPages;

[UIEvaluatePermission(SystemPermissions.VIEW)]
public class FormUsageListingTab(
    IInfoProvider<BizFormInfo> formProvider,
    IConversionService conversion,
    IPageUrlGenerator pageUrlGenerator) : ListingPage
{
    private readonly IInfoProvider<BizFormInfo> formProvider = formProvider;
    private readonly IConversionService conversion = conversion;
    private readonly IPageUrlGenerator pageUrlGenerator = pageUrlGenerator;

    [PageParameter(typeof(IntPageModelBinder), typeof(FormEditSection))]
    public int FormId { get; set; }

    protected override string ObjectType => ContentItemInfo.OBJECT_TYPE;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var form = await formProvider.GetAsync(FormId);

        PageConfiguration.QueryModifiers.AddModifier((query, s) =>
        {
            query
                .Source(source => source
                    .Join<WebPageItemInfo>(nameof(ContentItemInfo.ContentItemID), nameof(WebPageItemInfo.WebPageItemContentItemID))
                    .Join<ContentItemCommonDataInfo>(nameof(WebPageItemInfo.WebPageItemContentItemID), nameof(ContentItemCommonDataInfo.ContentItemCommonDataContentItemID))
                    .Join<ContentItemLanguageMetadataInfo>(nameof(ContentItemCommonDataInfo.ContentItemCommonDataContentItemID), nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentItemID),
                        additionalCondition: new WhereCondition($"CMS_ContentItemCommonData.ContentItemCommonDataContentLanguageID = CMS_ContentItemLanguageMetadata.ContentItemLanguageMetadataContentLanguageID"))
                    .Join<WebsiteChannelInfo>($"CMS_WebPageItem.{nameof(WebPageItemInfo.WebPageItemWebsiteChannelID)}", nameof(WebsiteChannelInfo.WebsiteChannelID))
                    .Join<ChannelInfo>(nameof(WebsiteChannelInfo.WebsiteChannelChannelID), nameof(ChannelInfo.ChannelID))
                    .Join<DataClassInfo>($"CMS_ContentItem.{nameof(ContentItemInfo.ContentItemContentTypeID)}", nameof(DataClassInfo.ClassID))
                    .Join<ContentLanguageInfo>($"CMS_ContentItemLanguageMetadata.{nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentLanguageID)}", nameof(ContentLanguageInfo.ContentLanguageID)))
                .WhereLike(
                    nameof(ContentItemCommonDataInfo.ContentItemCommonDataPageBuilderWidgets),
                    $$"""%{"selectedForm":[[]{"objectGuid":null,"objectCodeName":"{{form.FormName}}"}]%""")
                .AddColumn(nameof(WebsiteChannelInfo.WebsiteChannelID))
                .AddColumn(nameof(ContentLanguageInfo.ContentLanguageName));

            return query;
        });

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(WebPageItemInfo.WebPageItemID),
                "ID",
                maxWidth: 1)
            .AddComponentColumn(nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataDisplayName),
                "@kentico-community/portal-web-admin/Link",
                modelRetriever: WebPageLinkModelRetriever,
                caption: "Name",
                searchable: true,
                minWidth: 25)
            .AddColumn(nameof(DataClassInfo.ClassDisplayName),
                "Content Type",
                searchable: true,
                maxWidth: 5)
            .AddColumn(nameof(ContentLanguageInfo.ContentLanguageDisplayName),
                "Language",
                searchable: true,
                maxWidth: 5)
            .AddColumn(nameof(ChannelInfo.ChannelDisplayName),
                "Channel",
                searchable: true,
                maxWidth: 10)
            .AddColumn(nameof(WebPageItemInfo.WebPageItemTreePath),
                "Path",
                searchable: true,
                minWidth: 50);
    }

    private TableRowLinkProps WebPageLinkModelRetriever(object value, IDataContainer container)
    {
        int webPageItemID = conversion.GetInteger(container[nameof(WebPageItemInfo.WebPageItemID)], 0);
        int channelID = conversion.GetInteger(container[nameof(WebsiteChannelInfo.WebsiteChannelID)], 0);
        string languageName = conversion.GetString(container[nameof(ContentLanguageInfo.ContentLanguageName)], "");
        string valueStr = value.ToString() ?? "";

        if (webPageItemID == 0 || channelID == 0 || string.IsNullOrWhiteSpace(languageName))
        {
            return new TableRowLinkProps() { Label = valueStr, Path = "" };
        }

        string pageUrl = pageUrlGenerator.GenerateUrl<PageBuilderTab>($"webpages-{channelID}", $"{languageName}_{webPageItemID}");

        return new TableRowLinkProps()
        {
            Label = valueStr,
            Path = pageUrl.StartsWith('/')
                ? pageUrl[1..]
                : pageUrl
        };
    }
}
