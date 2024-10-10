using CMS.Base;
using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.DataEngine;
using CMS.Membership;
using CMS.Websites;
using CMS.Websites.Internal;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.Features.WebsiteChannels;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Filters;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Websites.UIPages;
using Kentico.Community.Portal.Admin.UIPages;

[assembly: UIPage(
    parentType: typeof(WebsiteChannelsApplicationPage),
    slug: "web-pages",
    uiPageType: typeof(WebPageListingPage),
    name: "Web Page List",
    templateName: TemplateNames.LISTING,
    order: 0,
    Icon = Icons.Magnifier)]

namespace Kentico.Community.Portal.Admin.Features.WebsiteChannels;

public class WebPageListingPage(IPageLinkGenerator pageLinkGenerator, IConversionService conversion) : ListingPage
{
    private readonly IPageLinkGenerator pageLinkGenerator = pageLinkGenerator;
    private readonly IConversionService conversion = conversion;

    protected override string ObjectType => ContentItemInfo.OBJECT_TYPE;

    /// <summary>
    /// Deletes user specified by the <paramref name="id"/> parameter.
    /// </summary>
    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.FilterFormModel = new WebPageListMultiFilter();

        PageConfiguration.QueryModifiers
            .AddModifier((query, settings) =>
            {
                return query
                    .Source(source => source
                        .Join<WebPageItemInfo>(nameof(ContentItemInfo.ContentItemID), nameof(WebPageItemInfo.WebPageItemContentItemID))
                        .Join<ContentItemCommonDataInfo>(nameof(WebPageItemInfo.WebPageItemContentItemID), nameof(ContentItemCommonDataInfo.ContentItemCommonDataContentItemID))
                        .Join<ContentItemLanguageMetadataInfo>(nameof(ContentItemCommonDataInfo.ContentItemCommonDataContentItemID), nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentItemID),
                            additionalCondition: new WhereCondition($"CMS_ContentItemCommonData.ContentItemCommonDataContentLanguageID = CMS_ContentItemLanguageMetadata.ContentItemLanguageMetadataContentLanguageID"))
                        .Join<WebsiteChannelInfo>($"CMS_WebPageItem.{nameof(WebPageItemInfo.WebPageItemWebsiteChannelID)}", nameof(WebsiteChannelInfo.WebsiteChannelID))
                        .Join<ChannelInfo>(nameof(WebsiteChannelInfo.WebsiteChannelChannelID), nameof(ChannelInfo.ChannelID))
                        .Join<DataClassInfo>($"CMS_ContentItem.{nameof(ContentItemInfo.ContentItemContentTypeID)}", nameof(DataClassInfo.ClassID))
                        .Join<ContentLanguageInfo>($"CMS_ContentItemLanguageMetadata.{nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentLanguageID)}", nameof(ContentLanguageInfo.ContentLanguageID)))
                    .AddColumn(nameof(WebsiteChannelInfo.WebsiteChannelID))
                    .AddColumn(nameof(ContentLanguageInfo.ContentLanguageName))
                    .AddColumn(nameof(DataClassInfo.ClassName));
            });

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(WebPageItemInfo.WebPageItemID),
                "ID",
                searchable: true,
                maxWidth: 1)
            .AddComponentColumn(nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataDisplayName),
                "@kentico-community/portal-web-admin/Link",
                modelRetriever: WebPageLinkModelRetriever,
                caption: "Name",
                searchable: true,
                minWidth: 25)
            .AddColumn(nameof(DataClassInfo.ClassDisplayName),
                "Content Type",
                maxWidth: 5)
            .AddColumn(nameof(ContentLanguageInfo.ContentLanguageDisplayName),
                "Language",
                maxWidth: 5)
            .AddColumn(nameof(ChannelInfo.ChannelDisplayName),
                "Channel",
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

        string pageUrl = pageLinkGenerator.GetPath<PageBuilderTab>(new()
        {
            { typeof(WebPageLayout), $"{languageName}_{webPageItemID}" },
            { typeof(WebPagesApplication), $"webpages-{channelID}" },
        });

        return new TableRowLinkProps()
        {
            Label = valueStr,
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

public class WebPageListMultiFilter
{
    [GeneralSelectorComponent(
        dataProviderType: typeof(ContentTypeGeneralSelectorDataProvider),
        Label = "Content Types",
        Placeholder = "Any",
        Order = 0
    )]
    [FilterCondition(
        BuilderType = typeof(GeneralWhereInWhereConditionBuilder),
        ColumnName = nameof(DataClassInfo.ClassName)
    )]
    public IEnumerable<string> ContentTypes { get; set; } = [];

    [GeneralSelectorComponent(
        dataProviderType: typeof(WebsiteChannelGeneralSelectorDataProvider),
        Label = "Channels",
        Placeholder = "Any",
        Order = 0
    )]
    [FilterCondition(
        BuilderType = typeof(GeneralWhereInWhereConditionBuilder),
        ColumnName = nameof(ChannelInfo.ChannelName)
    )]
    public IEnumerable<string> Channels { get; set; } = [];

    [GeneralSelectorComponent(
        dataProviderType: typeof(ContentLanguageGeneralSelectorDataProvider),
        Label = "Languages",
        Placeholder = "Any",
        Order = 0
    )]
    [FilterCondition(
        BuilderType = typeof(GeneralWhereInWhereConditionBuilder),
        ColumnName = nameof(ContentLanguageInfo.ContentLanguageName)
    )]
    public IEnumerable<string> Languages { get; set; } = [];
}

public class ContentLanguageGeneralSelectorDataProvider(IInfoProvider<ContentLanguageInfo> languageProvider)
    : IGeneralSelectorDataProvider
{
    private readonly IInfoProvider<ContentLanguageInfo> languageProvider = languageProvider;

    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Inavlid", Value = "" };

    public async Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        var items = await languageProvider.Get()
            .GetEnumerableTypedResultAsync();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            items = items.Where(i => i.ContentLanguageDisplayName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return new PagedSelectListItems<string>()
        {
            NextPageAvailable = false,
            Items = items.Select(i => new ObjectSelectorListItem<string> { IsValid = true, Text = i.ContentLanguageDisplayName, Value = i.ContentLanguageName }),
        };
    }

    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(IEnumerable<string> selectedValues, CancellationToken cancellationToken)
    {
        var items = await languageProvider.Get()
            .GetEnumerableTypedResultAsync();

        return (selectedValues ?? []).Select(GetSelectedItemByValue(items));

    }

    private Func<string, ObjectSelectorListItem<string>> GetSelectedItemByValue(IEnumerable<ContentLanguageInfo> languages) =>
        (string languageName) => languages
            .TryFirst(c => string.Equals(c.ContentLanguageName, languageName))
            .Map(c => new ObjectSelectorListItem<string> { IsValid = true, Text = c.ContentLanguageDisplayName, Value = c.ContentLanguageName })
            .GetValueOrDefault(InvalidItem);
}
