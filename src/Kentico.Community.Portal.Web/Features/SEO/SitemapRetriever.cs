using CMS.ContentEngine;
using CMS.Core;
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
    IConversionService conversion,
    ISystemClock clock)
{
    private readonly IProgressiveCache cache = cache;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IWebsiteChannelContext website = website;
    private readonly IContentQueryExecutor executor = executor;
    private readonly IConversionService conversion = conversion;
    private readonly ISystemClock clock = clock;

    private static readonly string[] contentTypeDependencies =
    [
        BlogLandingPage.CONTENT_TYPE_NAME,
        BlogPostPage.CONTENT_TYPE_NAME,
        CommunityLandingPage.CONTENT_TYPE_NAME,
        HomePage.CONTENT_TYPE_NAME,
        IntegrationsLandingPage.CONTENT_TYPE_NAME,
        LandingPage.CONTENT_TYPE_NAME,
        QAndALandingPage.CONTENT_TYPE_NAME,
        QAndANewQuestionPage.CONTENT_TYPE_NAME,
        QAndAQuestionPage.CONTENT_TYPE_NAME,
        ResourceHubPage.CONTENT_TYPE_NAME,
        SupportPage.CONTENT_TYPE_NAME
    ];

    public async Task<List<SitemapNode>> GetSitemapNodes() =>
        await cache.LoadAsync(cs =>
        {
            cs.CacheDependency = CacheHelper.GetCacheDependency(BuildCacheDependencyKeys());

            return GetSitemapNodesInternal();
        }, new CacheSettings(3, [nameof(GetSitemapNodes)]) { });

    private string[] BuildCacheDependencyKeys() =>
        contentTypeDependencies
            .Select(t => $"webpageitem|bychannel|{website.WebsiteChannelName}|bycontenttype|{t}")
            .ToArray();

    private async Task<List<SitemapNode>> GetSitemapNodesInternal()
    {
        var nodes = new List<SitemapNode>();

        var b = new ContentItemQueryBuilder()
            .ForContentTypes(c => c
                .OfReusableSchema(IWebPageMeta.REUSABLE_FIELD_SCHEMA_NAME)
                .ForWebsite(website.WebsiteChannelName))
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var pages = await executor.GetWebPageResult(b, c =>
        {
            /*
             * TryGetValue throws an exception when using the generic override and the retrieved value does not match the generic type
             * Using a "bool" out param would throw an exception when the value is null
             * so we use a nullable bool to match the null|true|false cases for this field.
             */
            bool isInSitemap = !c.TryGetValue(nameof(IWebPageMeta.WebPageMetaExcludeFromSitemap), out bool? val)
                || val is not bool isExcluded
                || !isExcluded;

            return new SitemapPage(new()
            {
                WebPageItemID = c.WebPageItemID,
                WebPageItemGUID = c.WebPageItemGUID,
                WebPageItemName = c.WebPageItemName,
                WebPageItemOrder = c.WebPageItemOrder,
                /*
                 * Accessing this field throws an exception
                 */
                // WebPageItemParentID = c.WebPageItemParentID,
                WebPageItemTreePath = c.WebPageItemTreePath,
                WebPageUrlPath = c.WebPageUrlPath,

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
                LastModified = clock.Now,
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
