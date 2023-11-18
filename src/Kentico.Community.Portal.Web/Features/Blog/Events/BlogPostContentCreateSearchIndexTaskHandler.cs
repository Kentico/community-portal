using CMS.ContentEngine;
using CMS.Core;
using CMS.Websites.Routing;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;

namespace Kentico.Community.Portal.Web.Features.Blog.Events;

/// <summary>
/// Ensures that new <see cref="BlogPostContent" /> records
/// trigger an index update of their associated blog post page since the Lucene
/// integration doesn't yet track object graphs
/// </summary>
public class BlogPostContentCreateSearchIndexTaskHandler
{
    private readonly IHttpContextAccessor accessor;
    private readonly IWebsiteChannelContext channelContext;
    private readonly IContentQueryExecutor executor;
    private readonly IEventLogService log;
    private readonly ILuceneTaskLogger taskLogger;

    public BlogPostContentCreateSearchIndexTaskHandler(
        IHttpContextAccessor accessor,
        IWebsiteChannelContext channelContext,
        IContentQueryExecutor executor,
        IEventLogService log,
        ILuceneTaskLogger taskLogger)
    {
        this.accessor = accessor;
        this.channelContext = channelContext;
        this.executor = executor;
        this.log = log;
        this.taskLogger = taskLogger;
    }

    public async Task Handle(PublishContentItemEventArgs args)
    {
        /*
         * Only perform search indexing when a site is available (eg not during CI restore)
         */
        if (accessor.HttpContext is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(channelContext.WebsiteChannelName))
        {
            return;
        }

        /*
         * BlogPostContent.SystemFields.ContentItemID
         */
        int contentItemID = args.ID;

        /*
         * We only need the values required for the IndexedItemModel, which come
         * from the BlogPostPage web page item that links to the updated BlogPostContent content item
         */
        var b = new ContentItemQueryBuilder()
            .ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParameters =>
            {
                _ = queryParameters
                    .ForWebsite(channelContext.WebsiteChannelName)
                    .Linking(nameof(BlogPostPage.BlogPostPageBlogPostContent), new[] { contentItemID })
                    .Columns(new[] { nameof(WebPageFields.WebPageItemGUID), nameof(WebPageFields.WebPageItemTreePath) });
            });

        var page = (await executor.GetWebPageResult(b, c => new { c.WebPageItemGUID, c.WebPageItemTreePath })).FirstOrDefault();
        if (page is null)
        {
            log.LogWarning(
                source: nameof(BlogPostContentCreateSearchIndexTaskHandler),
                eventCode: "MISSING_BLOGPOSTPAGE",
                eventDescription: $"Could not find blog web site page for blog content [{contentItemID}].{Environment.NewLine}Skipping search indexing.");

            return;
        }

        var model = new IndexedItemModel
        {
            ChannelName = channelContext.WebsiteChannelName,
            LanguageName = "en-US",
            TypeName = BlogPostPage.CONTENT_TYPE_NAME,
            WebPageItemGuid = page.WebPageItemGUID,
            WebPageItemTreePath = page.WebPageItemTreePath
        };
        await taskLogger.HandleEvent(model, WebPageEvents.Publish.Name);
    }
}
