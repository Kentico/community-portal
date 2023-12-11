using CMS.ContentEngine;
using CMS.Core;
using CMS.Helpers;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core;
using Kentico.Content.Web.Mvc;
using SimpleMvcSitemap;

namespace Kentico.Community.Portal.Web.Features.SEO;

public class Sitemap
{
    private readonly IProgressiveCache cache;
    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly IWebsiteChannelContext website;
    private readonly IContentQueryExecutor executor;
    private readonly IWebPageQueryResultMapper mapper;
    private readonly IConversionService conversion;
    private readonly ISystemClock clock;

    private static readonly string[] contentTypeDependencies = new[]
    {
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
    };

    public Sitemap(
        IProgressiveCache cache,
        IWebPageUrlRetriever urlRetriever,
        IWebsiteChannelContext website,
        IContentQueryExecutor executor,
        IWebPageQueryResultMapper mapper,
        IConversionService conversion,
        ISystemClock clock)
    {
        this.cache = cache;
        this.urlRetriever = urlRetriever;
        this.website = website;
        this.executor = executor;
        this.mapper = mapper;
        this.conversion = conversion;
        this.clock = clock;
    }

    public async Task<List<SitemapNode>> GetSitemapNodes() =>
        await cache.LoadAsync(cs =>
        {
            cs.CacheDependency = CacheHelper.GetCacheDependency(BuildCacheDependencyKeys());

            return GetSitemapNodesInternal();
        }, new CacheSettings(3, new[] { nameof(GetSitemapNodes) }) { });

    private string[] BuildCacheDependencyKeys() =>
        contentTypeDependencies
            .Select(t => $"webpageitem|bychannel|{website.WebsiteChannelName}|bycontenttype|{t}")
            .ToArray();

    private async Task<List<SitemapNode>> GetSitemapNodesInternal()
    {
        var nodes = new List<SitemapNode>();

        var b = new ContentItemQueryBuilder();

        foreach (string t in contentTypeDependencies)
        {
            b = b.ForContentType(t, c => c.ForWebsite(website.WebsiteChannelName))
                .InLanguage("en-US");
        }

        var pages = await executor.GetWebPageResult(b, c =>
        {
            /*
             * TryGetValue throws an exception when using the generic override and the retrieved value does not match the generic type
             * Using a "bool" out param would throw an exception when the value is null
             * so we use a nullable bool to match the null|true|false cases for this field.
             */
            bool isInSitemap = !c.TryGetValue(nameof(LandingPage.LandingPageIncludeInSitemap), out bool? val)
                || (val ?? true);

            return new SitemapPage
            {
                SystemFields = new()
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
                },
                IsInSitemap = isInSitemap
            };
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
                LastModificationDate = clock.Now,
                ChangeFrequency = ChangeFrequency.Weekly,
            };

            nodes.Add(node);
        }

        return nodes;
    }

    public class SitemapPage : IWebPageFieldsSource
    {
        public WebPageFields SystemFields { get; set; }
        public bool IsInSitemap { get; set; }
    }
}
