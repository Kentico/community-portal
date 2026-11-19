using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Web.Features.Integrations;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.SEO;

/// <summary>
/// Builds the <c>/llms.txt</c> index of resources for coding agents (#340).
/// </summary>
/// <remarks>
/// Indexes resources for developers working with Xperience, including digital marketing concepts, from live data:
/// <list type="bullet">
/// <item>Key pages: Q&amp;A, extensions and integrations, blog and resource hub landing pages</item>
/// <item>Topic hubs: <see cref="LandingPage"/> pages under a <see cref="ResourceHubPage"/> in the content tree</item>
/// <item>Q&amp;A discussion topics: DX Topic tags under the Technical and Product groups, linked to the filtered Q&amp;A listing</item>
/// <item>Integrations: every integration, linked to its repository</item>
/// <item>Product and development blog posts: the latest <see cref="BLOG_POST_LIMIT"/> posts tagged with a Technical or Product topic</item>
/// <item>Digital marketing blog posts: the latest <see cref="BLOG_POST_LIMIT"/> posts tagged with a Digital Marketing topic, minus those already listed</item>
/// </list>
/// Pages follow the sitemap exclusions: secured, "Sitemap - exclude" and noindex pages are left out.
/// Promotional and utility pages are never queried. Community and Refreshes posts are left out (news, not reference).
/// </remarks>
public class LlmsTxtRetriever(
    IProgressiveCache cache,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext website,
    IContentQueryExecutor executor,
    IMediator mediator,
    ICacheDependencyBuilderFactory cacheBuilderFactory)
{
    /// <summary>
    /// Bounds each blog section. Enough to cover recent, version-relevant posts while keeping the file
    /// small enough for an agent to load as context. Older posts stay reachable from the blog listing.
    /// </summary>
    public const int BLOG_POST_LIMIT = 50;

    /// <summary>
    /// DX Topic groups whose tags are developer topics. Digital Marketing, Community and Refreshes are not.
    /// </summary>
    public static IReadOnlyList<Guid> DeveloperTopicGroupGUIDs { get; } =
    [
        new("54a6a93a-d53b-449d-ad49-7b07c77e5518"), // Technical
        new("0b545890-df0f-4340-8643-e32943c86c1a"), // Product
    ];

    /// <summary>
    /// DX Topic group for digital marketing concepts developers working with Xperience should understand
    /// </summary>
    public static Guid DigitalMarketingTopicGroupGUID { get; } = new("f60214d8-5399-4afc-92aa-3792c8e40f3f");

    private readonly IProgressiveCache cache = cache;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IWebsiteChannelContext website = website;
    private readonly IContentQueryExecutor executor = executor;
    private readonly IMediator mediator = mediator;
    private readonly ICacheDependencyBuilderFactory cacheBuilderFactory = cacheBuilderFactory;

    public async Task<LlmsTxtDocument> GetDocument() =>
        await cache.LoadAsync(cs =>
        {
            cs.CacheDependency = cacheBuilderFactory.Create()
                .ForWebPageItems().All(website.WebsiteChannelName).Builder()
                .ForContentItems().ByContentType(IntegrationContent.CONTENT_TYPE_NAME).Builder()
                .ForInfoObjects<TagInfo>().All().Builder()
                .Build();

            return GetDocumentInternal();
        }, new CacheSettings(60, [nameof(LlmsTxtRetriever), website.WebsiteChannelName]));

    private async Task<LlmsTxtDocument> GetDocumentInternal()
    {
        var taxonomies = await mediator.Send(new QAndATaxonomiesQuery());
        var developerTopics = taxonomies.DXTopicsHierarchy
            .Where(group => DeveloperTopicGroupGUIDs.Contains(group.Guid))
            .ToList();
        var marketingTopicGUIDs = taxonomies.DXTopicsHierarchy
            .Where(group => group.Guid == DigitalMarketingTopicGroupGUID)
            .SelectMany(g => g.Children)
            .Select(t => t.Guid);

        var keyPages = await GetKeyPages();
        string qAndAPath = keyPages.TryFirst(p => p.Page is QAndALandingPage).Map(p => p.Link.URL).GetValueOrDefault("");
        string integrationsPath = keyPages.TryFirst(p => p.Page is IntegrationsLandingPage).Map(p => p.Link.URL).GetValueOrDefault("");

        var developerPosts = await GetBlogPosts(developerTopics.SelectMany(g => g.Children).Select(t => t.Guid));
        var listedPostURLs = developerPosts.Select(l => l.URL).ToHashSet();
        var marketingPosts = (await GetBlogPosts(marketingTopicGUIDs))
            .Where(l => !listedPostURLs.Contains(l.URL))
            .ToList();

        return new([
            new("Key pages", [.. keyPages.Select(p => p.Link)]),
            new("Topic hubs", await GetTopicHubs(keyPages.Where(p => p.Page is ResourceHubPage))),
            new("Q&A discussion topics", GetQAndATopics(developerTopics, qAndAPath)),
            new("Extensions and Integrations", await GetIntegrations(integrationsPath)),
            new("Product and development blog posts", developerPosts),
            new("Digital marketing blog posts", marketingPosts),
        ]);
    }

    private async Task<IReadOnlyList<IndexedPage>> GetKeyPages()
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(c => c
                .OfContentType(
                    QAndALandingPage.CONTENT_TYPE_NAME,
                    IntegrationsLandingPage.CONTENT_TYPE_NAME,
                    BlogLandingPage.CONTENT_TYPE_NAME,
                    ResourceHubPage.CONTENT_TYPE_NAME)
                .WithContentTypeFields()
                .ForWebsite(website.WebsiteChannelName))
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var pages = await executor.GetMappedWebPageResult<IWebPageFieldsSource>(b);

        // Developer entry points first, in a stable order
        return [.. (await ToIndexedPages(pages)).OrderBy(p => KeyPageOrder(p.Page))];
    }

    /// <summary>
    /// Topic hub hook: a hub is a <see cref="LandingPage"/> placed anywhere under a <see cref="ResourceHubPage"/>.
    /// Publishing a hub there adds it here; marketing and utility landing pages live elsewhere in the tree.
    /// </summary>
    private async Task<IReadOnlyList<LlmsTxtLink>> GetTopicHubs(IEnumerable<IndexedPage> resourceHubs)
    {
        var links = new List<LlmsTxtLink>();

        foreach (var hub in resourceHubs)
        {
            var b = new ContentItemQueryBuilder()
                .ForContentType(LandingPage.CONTENT_TYPE_NAME, q => q
                    .ForWebsite(website.WebsiteChannelName, PathMatch.Children(hub.Page.SystemFields.WebPageItemTreePath))
                    .OrderBy(nameof(WebPageFields.WebPageItemOrder)))
                .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

            var pages = await executor.GetMappedWebPageResult<LandingPage>(b);

            links.AddRange((await ToIndexedPages(pages)).Select(p => p.Link));
        }

        return links;
    }

    private static List<LlmsTxtLink> GetQAndATopics(IEnumerable<Core.Modules.TaxonomyTag> developerTopicGroups, string qAndAPath)
    {
        if (string.IsNullOrWhiteSpace(qAndAPath))
        {
            return [];
        }

        // Same query string the Q&A listing's topic filter submits
        return [.. developerTopicGroups
            .SelectMany(group => group.Children.Select(topic => new LlmsTxtLink(
                $"{group.DisplayName}: {topic.DisplayName}",
                $"{qAndAPath.TrimEnd('/')}?dxTopics={Uri.EscapeDataString(topic.NormalizedName)}",
                $"Community questions and answers about {topic.DisplayName}")))];
    }

    private async Task<List<LlmsTxtLink>> GetIntegrations(string integrationsPath)
    {
        var resp = await mediator.Send(new IntegrationContentsQuery());

        return [.. resp.Items
            .Select(i => i.Content)
            .OrderBy(c => c.BasicItemTitle, StringComparer.OrdinalIgnoreCase)
            .Select(c => Maybe.From(c.IntegrationContentRepositoryLinkURL)
                .MapNullOrWhiteSpaceAsNone()
                .Map(IntegrationItemViewModel.NormalizeRepositoryURL)
                .Or(Maybe.From(c.IntegrationContentLibraryLinkURL).MapNullOrWhiteSpaceAsNone())
                .Or(string.IsNullOrWhiteSpace(integrationsPath)
                    ? Maybe<string>.None
                    : $"{integrationsPath}#{IntegrationItemViewModel.AnchorFor(c)}")
                .Map(url => new LlmsTxtLink(c.BasicItemTitle, url, c.BasicItemShortDescription)))
            .Where(l => l.HasValue)
            .Select(l => l.Value)];
    }

    private async Task<List<LlmsTxtLink>> GetBlogPosts(IEnumerable<Guid> topics)
    {
        var topicGUIDs = topics.ToArray();
        if (topicGUIDs.Length == 0)
        {
            return [];
        }

        var b = new ContentItemQueryBuilder()
            .ForContentType(BlogPostPage.CONTENT_TYPE_NAME, q => q
                .ForWebsite(website.WebsiteChannelName)
                .Where(w => w.WhereContainsTags(nameof(BlogPostPage.CoreTaxonomyDXTopics), topicGUIDs))
                .OrderBy(new OrderByColumn(nameof(BlogPostPage.BlogPostPagePublishedDate), OrderDirection.Descending))
                .TopN(BLOG_POST_LIMIT))
            .InLanguage(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        var pages = await executor.GetMappedWebPageResult<BlogPostPage>(b);

        return [.. (await ToIndexedPages(pages)).Select(p => p.Link)];
    }

    /// <summary>
    /// Applies the sitemap exclusions and resolves URLs. Secured pages are already excluded by the query defaults.
    /// </summary>
    private async Task<List<IndexedPage>> ToIndexedPages(IEnumerable<IWebPageFieldsSource> pages)
    {
        var indexed = new List<IndexedPage>();

        foreach (var page in pages)
        {
            if (page is not IWebPageMetaFields metaFields || !IsIndexable(metaFields))
            {
                continue;
            }

            var meta = new WebPageMeta(metaFields);
            var url = await urlRetriever.Retrieve(page);

            indexed.Add(new(page, new(meta.Title, url.RelativePathTrimmed(), meta.Description)));
        }

        return indexed;
    }

    private static bool IsIndexable(IWebPageMetaFields meta) =>
        !meta.WebPageMetaExcludeFromSitemap
        && !(meta.WebPageMetaRobots ?? "").Contains("noindex", StringComparison.OrdinalIgnoreCase);

    private static int KeyPageOrder(IWebPageFieldsSource page) => page switch
    {
        QAndALandingPage => 0,
        IntegrationsLandingPage => 1,
        BlogLandingPage => 2,
        _ => 3
    };

    private record IndexedPage(IWebPageFieldsSource Page, LlmsTxtLink Link);
}
