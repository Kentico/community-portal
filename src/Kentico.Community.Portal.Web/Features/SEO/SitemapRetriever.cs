using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core;
using Kentico.Content.Web.Mvc;
using Sidio.Sitemap.Core;

namespace Kentico.Community.Portal.Web.Features.SEO;

public class SitemapRetriever(
    IProgressiveCache cache,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext website,
    IContentQueryExecutor executor,
    ICacheDependencyBuilderFactory cacheBuilderFactory)
{
    private readonly IProgressiveCache cache = cache;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IWebsiteChannelContext website = website;
    private readonly IContentQueryExecutor executor = executor;
    private readonly ICacheDependencyBuilderFactory cacheBuilderFactory = cacheBuilderFactory;

    public async Task<List<SitemapNode>> GetSitemapNodes() =>
        await cache.LoadAsync(cs =>
        {
            var builder = cacheBuilderFactory.Create();

            cs.CacheDependency = builder.ForWebPageItems().All(website.WebsiteChannelName).Builder().Build();

            return GetSitemapNodesInternal();
        }, new CacheSettings(60, [nameof(GetSitemapNodes)]) { });

    private async Task<List<SitemapNode>> GetSitemapNodesInternal()
    {
        var nodes = new List<SitemapNode>();

        var b = new ContentItemQueryBuilder()
            .ForContentTypes(c => c
                .OfReusableSchema(IWebPageMetaFields.REUSABLE_FIELD_SCHEMA_NAME)
                .ForWebsite(website.WebsiteChannelName))
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var pages = await executor.GetWebPageResult(b, c =>
        {
            bool isInSitemap = !c.TryGetValue(nameof(IWebPageMetaFields.WebPageMetaExcludeFromSitemap), out bool val) || !val;

            return new SitemapPage(new()
            {
                WebPageItemID = c.WebPageItemID,
                WebPageItemGUID = c.WebPageItemGUID,
                WebPageItemName = c.WebPageItemName,
                WebPageItemOrder = c.WebPageItemOrder,
                WebPageItemParentID = c.WebPageItemParentID,
                WebPageItemTreePath = c.WebPageItemTreePath,
                WebPageUrlPath = c.WebPageUrlPath,
                WebPageItemWebsiteChannelId = c.WebPageItemWebsiteChannelID,
                WebPageUrlPathContentLanguageID = c.ContentItemCommonDataContentLanguageID,

                ContentItemCommonDataLastPublishedWhen = c.ContentItemCommonDataLastPublishedWhen,
                ContentItemCommonDataContentLanguageID = c.ContentItemCommonDataContentLanguageID,
                ContentItemCommonDataVersionStatus = c.ContentItemCommonDataVersionStatus,
                ContentItemContentTypeID = c.ContentItemContentTypeID,
                ContentItemGUID = c.ContentItemGUID,
                ContentItemID = c.ContentItemID,
                ContentItemIsSecured = c.ContentItemIsSecured,
                ContentItemName = c.ContentItemName
            }, isInSitemap);
        });

        foreach (var page in pages)
        {
            if (!page.IsInSitemap)
            {
                continue;
            }

            var pageUrl = await urlRetriever.Retrieve(page);

            var node = new SitemapNode(pageUrl.RelativePathTrimmed())
            {
                LastModified = page.SystemFields.ContentItemCommonDataLastPublishedWhen,
                ChangeFrequency = ChangeFrequency.Weekly,
            };

            nodes.Add(node);
        }

        return nodes;
    }

    public struct SitemapPage(WebPageFields systemFields, bool isInSitemap) : IWebPageFieldsSource
    {
        public WebPageFields SystemFields { get; set; } = systemFields;
        public bool IsInSitemap { get; set; } = isInSitemap;
    }
}
