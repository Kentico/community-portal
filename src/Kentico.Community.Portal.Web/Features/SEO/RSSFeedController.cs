using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using CMS.Helpers;
using CMS.Membership;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Features.SEO;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    contentTypeName: RSSFeedPage.CONTENT_TYPE_NAME,
    controllerType: typeof(RSSFeedController),
    ActionName = nameof(RSSFeedController.RSSFeed))]

namespace Kentico.Community.Portal.Web.Features.SEO;

public class RSSFeedController(
    IMediator mediator,
    IWebPageDataContextRetriever webPageDataContextRetriever,
    IWebPageUrlRetriever webPageUrlRetriever,
    TimeProvider clock,
    IProgressiveCache cache,
    ICacheDependencyKeysBuilder keysBuilder,
    IWebsiteChannelContext channelContext) : Controller
{
    private readonly IMediator mediator = mediator;
    private readonly IWebPageDataContextRetriever webPageDataContextRetriever = webPageDataContextRetriever;
    private readonly IWebPageUrlRetriever webPageUrlRetriever = webPageUrlRetriever;
    private readonly TimeProvider clock = clock;
    private readonly IProgressiveCache cache = cache;
    private readonly ICacheDependencyKeysBuilder keysBuilder = keysBuilder;
    private readonly IWebsiteChannelContext channelContext = channelContext;

    [ResponseCache(Duration = 1200)]
    public async Task<ActionResult> RSSFeed()
    {
        if (!webPageDataContextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var feedPage = await mediator.Send(new RSSFeedPageQuery(data.WebPage));
        if (!string.Equals(feedPage.RSSFeedPageWebPageContentType, BlogPostPage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(feedPage.RSSFeedPageWebPageContentType, QAndAQuestionPage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound("Only blog post and Q&A question RSS feeds are currently enabled");
        }

        var feed = await GetFeed(feedPage);

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

    private async Task<SyndicationFeed> GetFeed(RSSFeedPage feedPage)
    {
        var utcNow = clock.GetUtcNow().DateTime;
        var feedURL = await webPageUrlRetriever.Retrieve(feedPage);
        var feed = new SyndicationFeed(
            feedPage.RSSFeedPageFeedName,
            feedPage.RSSFeedPageFeedShortDescription,
            new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}{feedURL.RelativePathTrimmed()}"),
            "RSSUrl",
            utcNow)
        {
            Copyright = new TextSyndicationContent($"{utcNow.Year} - Kentico")
        };

        if (string.Equals(feedPage.RSSFeedPageWebPageContentType, BlogPostPage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            var tags = await mediator.Send(new BlogPostTaxonomiesQuery());
            return await cache.LoadAsync(cs => BlogPostRSSFeedInternal(feed, feedPage, tags), GetCacheSettings(feedPage));
        }
        else if (string.Equals(feedPage.RSSFeedPageWebPageContentType, QAndAQuestionPage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            var members = await mediator.Send(new MembersAllQuery());
            var tags = await mediator.Send(new QAndATaxonomiesQuery());
            var resp = await mediator.Send(new QAndAQuestionPagesLatestQuery(feedPage.RSSFeedPageItemsLimit, channelContext.WebsiteChannelName));
            return await cache.LoadAsync(cs => QAndAQuestionPageInternal(feed, tags, members, resp), GetCacheSettings(feedPage));
        }

        return new SyndicationFeed();
    }

    private CacheSettings GetCacheSettings(RSSFeedPage feedPage) =>
        new(5, [nameof(RSSFeed), feedPage.RSSFeedPageWebPageContentType])
        {
            GetCacheDependency = () =>
            {
                _ = keysBuilder.WebPage(feedPage);

                if (string.Equals(feedPage.RSSFeedPageWebPageContentType, BlogPostPage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    _ = keysBuilder
                        .AllContentItems(BlogPostPage.CONTENT_TYPE_NAME)
                        .AllContentItems(BlogPostContent.CONTENT_TYPE_NAME);
                }
                else if (string.Equals(feedPage.RSSFeedPageWebPageContentType, QAndAQuestionPage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    _ = keysBuilder.AllContentItems(QAndAQuestionPage.CONTENT_TYPE_NAME);
                }

                var keys = (keysBuilder as CacheDependencyKeysBuilder)?.GetKeys();

                return CacheHelper.GetCacheDependency(keys);
            }
        };

    private async Task<SyndicationFeed> BlogPostRSSFeedInternal(SyndicationFeed feed, RSSFeedPage feedPage, BlogPostTaxonomiesQueryResponse blogTags)
    {
        var postPagesResult = await mediator.Send(new BlogPostPagesLatestQuery(feedPage.RSSFeedPageItemsLimit, channelContext.WebsiteChannelName));
        var items = new List<SyndicationItem>();
        foreach (var page in postPagesResult.Items)
        {
            if (page.BlogPostPageBlogPostContent.FirstOrDefault() is not BlogPostContent post)
            {
                continue;
            }

            var postURL = await webPageUrlRetriever.Retrieve(page);
            string title = page.BasicItemTitle;
            string description = page.BasicItemShortDescription;
            var absoluteURI = new Uri(postURL.WebPageAbsoluteURL(Request));
            string pageID = page.SystemFields.WebPageItemGUID.ToString("N");
            var item = new SyndicationItem(title, description, absoluteURI, pageID, new(page.SystemFields.ContentItemCommonDataLastPublishedWhen ?? page.BlogPostPagePublishedDate))
            {
                PublishDate = page.BlogPostPagePublishedDate
            };

            page.BlogPostPageAuthorContent
                .TryFirst()
                .Execute(author =>
                {
                    item.Authors.Add(new SyndicationPerson() { Name = author.FullName });
                });

            post.BlogPostContentBlogType
                .TryFirst()
                .Bind(tr => blogTags.Types.TryFirst(i => i.Guid == tr.Identifier))
                .Execute(tag => item.Categories.Add(new SyndicationCategory(tag.DisplayName)));

            items.Add(item);
        }

        feed.Items = items;

        return feed;
    }

    private async Task<SyndicationFeed> QAndAQuestionPageInternal(
        SyndicationFeed feed,
        QAndATaxonomiesQueryResponse qAndATags,
        Dictionary<int, MemberInfo> members,
        QAndAQuestionPagesLatestQueryResponse resp)
    {
        var items = new List<SyndicationItem>();
        foreach (var page in resp.Items)
        {
            var postURL = await webPageUrlRetriever.Retrieve(page);
            string title = Maybe.From(page.BasicItemTitle)
                .MapNullOrWhiteSpaceAsNone()
                .IfNoValue(page.WebPageMetaTitle)
                .GetValueOrDefault("");
            string description = Maybe.From(page.BasicItemShortDescription)
                .MapNullOrWhiteSpaceAsNone()
                .IfNoValue(page.WebPageMetaShortDescription)
                .GetValueOrDefault("");
            var absoluteURI = new Uri(postURL.WebPageAbsoluteURL(Request));
            string pageID = page.SystemFields.WebPageItemGUID.ToString("N");
            var item = new SyndicationItem(title, description, absoluteURI, pageID, page.QAndAQuestionPageDateModified)
            {
                PublishDate = page.QAndAQuestionPageDateCreated
            };

            if (members.TryGetValue(page.QAndAQuestionPageAuthorMemberID, out var member))
            {
                item.Authors.Add(new SyndicationPerson() { Name = member.MemberName });
            }

            page.CoreTaxonomyDXTopics
                .Select(t => qAndATags.DXTopicsAll.TryFirst(i => i.Guid == t.Identifier))
                .ToList()
                .ForEach(i => i.Execute(tag => item.Categories.Add(new SyndicationCategory(tag.DisplayName))));

            items.Add(item);
        }

        feed.Items = items;

        return feed;
    }
}
