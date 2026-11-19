using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.SEO.IndexNow;

/// <summary>
/// Turns queued pages into absolute URLs, keeping only pages a search engine should index -
/// the same rules as the sitemap (see <see cref="SitemapRetriever"/>)
/// </summary>
public class IndexNowUrlResolver(
    IContentQueryExecutor executor,
    IWebPageUrlRetriever urlRetriever)
{
    private readonly IContentQueryExecutor executor = executor;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;

    public async Task<IReadOnlyList<string>> GetIndexableURLs(IReadOnlyCollection<IndexNowPage> pages, CancellationToken cancellationToken)
    {
        var urls = new List<string>();

        foreach (var group in pages.GroupBy(p => (p.WebsiteChannelName, p.LanguageName)))
        {
            int[] ids = [.. group.Select(p => p.WebPageItemID).Distinct()];
            var candidates = await GetCandidates(ids, group.Key.LanguageName, cancellationToken);
            var blogDiscussionIDs = await GetBlogDiscussionIDs(ids, group.Key.LanguageName, cancellationToken);

            foreach (var page in candidates)
            {
                if (!IndexNowEligibility.IsIndexable(page.ExcludeFromSitemap, page.Robots, blogDiscussionIDs.Contains(page.WebPageItemID)))
                {
                    continue;
                }

                var url = await urlRetriever.Retrieve(page.WebPageItemID, group.Key.LanguageName, false, cancellationToken);
                if (Uri.TryCreate(url.AbsoluteUrl, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                {
                    urls.Add(uri.AbsoluteUri);
                }
            }
        }

        return urls;
    }

    /// <summary>
    /// Published pages with SEO metadata. Secured pages are excluded by the query defaults.
    /// </summary>
    private async Task<IEnumerable<Candidate>> GetCandidates(int[] webPageItemIDs, string languageName, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(c => c
                .OfReusableSchema(IWebPageMetaFields.REUSABLE_FIELD_SCHEMA_NAME)
                .ForWebsite(webPageItemIDs))
            .InLanguage(languageName);

        return await executor.GetWebPageResult(b, c => new Candidate(
            c.WebPageItemID,
            c.TryGetValue(nameof(IWebPageMetaFields.WebPageMetaExcludeFromSitemap), out bool exclude) && exclude,
            c.TryGetValue(nameof(IWebPageMetaFields.WebPageMetaRobots), out string? robots) ? robots : null),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Blog discussions serve noindex
    /// </summary>
    private async Task<HashSet<int>> GetBlogDiscussionIDs(int[] webPageItemIDs, string languageName, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(c => c
                .OfContentType(QAndAQuestionPage.CONTENT_TYPE_NAME)
                .ForWebsite(webPageItemIDs)
                .WithContentTypeFields())
            .InLanguage(languageName)
            .Parameters(q => q
                .Where(w => w.WhereContainsTags(
                    nameof(QAndAQuestionPage.QAndAQuestionPageDiscussionType),
                    [SystemTaxonomies.QAndADiscussionTypeTaxonomy.Blog.TagGUID])));

        var ids = await executor.GetWebPageResult(b, c => c.WebPageItemID, cancellationToken: cancellationToken);

        return [.. ids];
    }

    private record Candidate(int WebPageItemID, bool ExcludeFromSitemap, string? Robots);
}

public static class IndexNowEligibility
{
    /// <summary>
    /// A page is submitted only when it is listed in the sitemap and not marked noindex
    /// </summary>
    public static bool IsIndexable(bool excludeFromSitemap, string? robots, bool isBlogDiscussion) =>
        !excludeFromSitemap
        && !isBlogDiscussion
        && (robots is null || !robots.Contains("noindex", StringComparison.OrdinalIgnoreCase));
}
