using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Content.Web.Mvc;
using Sidio.Sitemap.Core;

namespace Kentico.Community.Portal.Web.Features.SEO;

public class SitemapRetriever(
    IProgressiveCache cache,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext website,
    IContentQueryExecutor executor,
    IInfoProvider<QAndAAnswerDataInfo> answerProvider,
    ICacheDependencyBuilderFactory cacheBuilderFactory)
{
    private readonly IProgressiveCache cache = cache;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IWebsiteChannelContext website = website;
    private readonly IContentQueryExecutor executor = executor;
    private readonly IInfoProvider<QAndAAnswerDataInfo> answerProvider = answerProvider;
    private readonly ICacheDependencyBuilderFactory cacheBuilderFactory = cacheBuilderFactory;

    public async Task<List<SitemapNode>> GetSitemapNodes() =>
        await cache.LoadAsync(cs =>
        {
            var builder = cacheBuilderFactory.Create();

            cs.CacheDependency = builder
                .ForWebPageItems().All(website.WebsiteChannelName).Builder()
                .ForInfoObjects<QAndAAnswerDataInfo>().All().Builder()
                .Build();

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

        var latestAnswers = await GetLatestAnswerDates();
        var blogDiscussionIDs = await GetBlogDiscussionIDs();

        foreach (var page in pages)
        {
            // Secured pages are not returned: the query excludes them by default
            // Blog discussions serve noindex, so listing them would send mixed signals
            if (!page.IsInSitemap || blogDiscussionIDs.Contains(page.SystemFields.WebPageItemID))
            {
                continue;
            }

            var pageUrl = await urlRetriever.Retrieve(page);

            // No changefreq: there is no real per-URL signal and search engines ignore it.
            var lastModified = page.SystemFields.ContentItemCommonDataLastPublishedWhen;

            // A discussion changes when it is answered, not only when the question is published
            if (latestAnswers.TryGetValue(page.SystemFields.WebPageItemID, out var latestAnswer)
                && (lastModified is null || latestAnswer > lastModified))
            {
                lastModified = latestAnswer;
            }

            var node = new SitemapNode(pageUrl.RelativePathTrimmed())
            {
                LastModified = lastModified,
            };

            nodes.Add(node);
        }

        return nodes;
    }

    private async Task<HashSet<int>> GetBlogDiscussionIDs()
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(c => c
                .OfContentType(QAndAQuestionPage.CONTENT_TYPE_NAME)
                .ForWebsite(website.WebsiteChannelName)
                .WithContentTypeFields())
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE)
            .Parameters(q => q
                .Where(w => w.WhereContainsTags(
                    nameof(QAndAQuestionPage.QAndAQuestionPageDiscussionType),
                    [SystemTaxonomies.QAndADiscussionTypeTaxonomy.Blog.TagGUID])));

        var ids = await executor.GetWebPageResult(b, c => c.WebPageItemID);

        return [.. ids];
    }

    private async Task<Dictionary<int, DateTime>> GetLatestAnswerDates()
    {
        var answers = await answerProvider.Get()
            .Columns(nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID), nameof(QAndAAnswerDataInfo.QAndAAnswerDataDateModified))
            .GetEnumerableTypedResultAsync();

        return answers
            .GroupBy(a => a.QAndAAnswerDataQuestionWebPageItemID)
            .ToDictionary(g => g.Key, g => g.Max(a => a.QAndAAnswerDataDateModified));
    }

    public struct SitemapPage(WebPageFields systemFields, bool isInSitemap) : IWebPageFieldsSource
    {
        public WebPageFields SystemFields { get; set; } = systemFields;
        public bool IsInSitemap { get; set; } = isInSitemap;
    }
}
