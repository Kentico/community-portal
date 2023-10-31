using System.Text;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.Community.Portal.Web.Features.SEO;
using Kentico.Content.Web.Mvc;
using Kentico.Community.Portal.Web.Features.Blog;
using System.Xml;
using System.ServiceModel.Syndication;
using Kentico.Community.Portal.Core;
using System.Xml.Linq;
using CMS.MediaLibrary;
using Kentico.Community.Portal.Web.Rendering;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Operations;

[assembly: RegisterWebPageRoute(
    contentTypeName: RSSFeedPage.CONTENT_TYPE_NAME,
    controllerType: typeof(RSSFeedController),
    ActionName = nameof(RSSFeedController.RSSFeed))]

namespace Kentico.Community.Portal.Web.Features.SEO;

public class RSSFeedController : Controller
{
    private readonly IMediator mediator;
    private readonly IWebPageDataContextRetriever webPageDataContextRetriever;
    private readonly IWebPageUrlRetriever webPageUrlRetriever;
    private readonly AssetItemService assetService;
    private readonly ISystemClock clock;
    private readonly IProgressiveCache cache;
    private readonly ICacheDependencyKeysBuilder keysBuilder;

    public RSSFeedController(
        IMediator mediator,
        IWebPageDataContextRetriever webPageDataContextRetriever,
        IWebPageUrlRetriever webPageUrlRetriever,
        AssetItemService assetService,
        ISystemClock clock,
        IProgressiveCache cache,
        ICacheDependencyKeysBuilder keysBuilder)
    {
        this.mediator = mediator;
        this.webPageDataContextRetriever = webPageDataContextRetriever;
        this.webPageUrlRetriever = webPageUrlRetriever;
        this.assetService = assetService;
        this.clock = clock;
        this.cache = cache;
        this.keysBuilder = keysBuilder;
    }

    [ResponseCache(Duration = 1200)]
    public async Task<ActionResult> RSSFeed()
    {
        if (!webPageDataContextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var feedPage = await mediator.Send(new RSSFeedPageQuery(data.WebPage));

        if (!string.Equals(feedPage.RSSFeedPageWebPageContentType, BlogPostPage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound("Only blog post RSS feeds are currently enabled");
        }

        var cacheSettings = new CacheSettings(5, new[] { nameof(RSSFeed), feedPage.RSSFeedPageWebPageContentType })
        {
            GetCacheDependency = () =>
            {
                _ = keysBuilder.WebPage(feedPage).AllContentItems(BlogPostPage.CONTENT_TYPE_NAME).AllContentItems(BlogPostContent.CONTENT_TYPE_NAME);

                var keys = (keysBuilder as CacheDependencyKeysBuilder).GetKeys();

                return CacheHelper.GetCacheDependency(keys);
            }
        };
        var feed = await cache.LoadAsync(cs => RSSFeedInternal(feedPage), cacheSettings);

        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            NewLineHandling = NewLineHandling.Entitize,
            NewLineOnAttributes = true,
            Indent = true
        };

        using var stream = new MemoryStream();
        using (var xmlWriter = XmlWriter.Create(stream, settings))
        {
            var rssFormatter = new Rss20FeedFormatter(feed, false);
            rssFormatter.WriteTo(xmlWriter);
            xmlWriter.Flush();
        }

        return File(stream.ToArray(), "application/rss+xml; charset=utf-8");
    }

    private async Task<SyndicationFeed> RSSFeedInternal(RSSFeedPage feedPage)
    {
        var feedURL = await webPageUrlRetriever.Retrieve(feedPage);

        var postPagesResult = await mediator.Send(new BlogPostPagesLatestQuery(feedPage.RSSFeedPageItemsLimit));

        var feed = new SyndicationFeed(
            feedPage.RSSFeedPageFeedName,
            feedPage.RSSFeedPageFeedShortDescription,
            new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}{feedURL.RelativePathTrimmed()}"),
            "RSSUrl",
            clock.UtcNow)
        {
            Copyright = new TextSyndicationContent($"{clock.UtcNow.Year} - Kentico")
        };
        var items = new List<SyndicationItem>();
        foreach (var postPage in postPagesResult.Items)
        {
            if (postPage.BlogPostPageBlogPostContent.FirstOrDefault() is not BlogPostContent post)
            {
                continue;
            }

            var postURL = await webPageUrlRetriever.Retrieve(postPage);
            string title = postPage.BlogPostPageTitle;
            string description = postPage.BlogPostPageShortDescription;
            var absoluteURI = new Uri(postURL.AbsoluteURL(Request));
            string pageID = postPage.SystemFields.WebPageItemGUID.ToString("N");
            var item = new SyndicationItem(title, description, absoluteURI, pageID, post.BlogPostContentPublishedDate)
            {
                PublishDate = post.BlogPostContentPublishedDate
            };

            post.BlogPostContentAuthor.TryFirst()
                .Execute(author =>
                {
                    item.Authors.Add(new SyndicationPerson() { Name = author.FullName });
                });

            item.Categories.Add(new SyndicationCategory(post.BlogPostContentTaxonomy));

            if (post.BlogPostContentTeaserMediaFileImage.FirstOrDefault() is AssetRelatedItem asset)
            {
                var image = await assetService.RetrieveMediaFileImage(asset);

                item.ElementExtensions.Add(new XElement("image", image.URLData.AbsoluteURL(Request)));
            }

            items.Add(item);
        }

        feed.Items = items;

        return feed;
    }
}
