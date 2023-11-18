using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using SimpleMvcSitemap;

namespace Kentico.Community.Portal.Web.Features.SEO;

public class Sitemap
{
    private readonly IProgressiveCache cache;
    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly IWebsiteChannelContext website;
    private readonly IContentQueryExecutor executor;
    private readonly IWebPageQueryResultMapper mapper;
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

    public Sitemap(IProgressiveCache cache, IWebPageUrlRetriever urlRetriever, IWebsiteChannelContext website, IContentQueryExecutor executor, IWebPageQueryResultMapper mapper)
    {
        this.cache = cache;
        this.urlRetriever = urlRetriever;
        this.website = website;
        this.executor = executor;
        this.mapper = mapper;
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
            b.ForContentType(t, c => c.ForWebsite(website.WebsiteChannelName).UrlPathColumns());
        }

        var pages = await executor.GetWebPageResult(b, c =>
        {
            return new SitemapPage();
        });

        foreach (var item in pages)
        {
            // if (item is LandingPage landingPage)
            // {
            //     if (!landingPage.Fields.IncludeInSitemap)
            //     {
            //         continue;
            //     }
            // }

            var pageUrl = await urlRetriever.Retrieve(item);

            var node = new SitemapNode(pageUrl.RelativePath)
            {
                LastModificationDate = DateTime.Now,
                ChangeFrequency = ChangeFrequency.Daily,
            };

            nodes.Add(node);
        }

        return nodes;
    }

    public class SitemapPage : IWebPageFieldsSource
    {
        public WebPageFields SystemFields { get; set; }
    }
}
