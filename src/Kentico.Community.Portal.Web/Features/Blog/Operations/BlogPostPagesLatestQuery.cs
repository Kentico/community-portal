using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostPagesLatestQuery(int Count, string ChannelName) : IQuery<BlogPostPagesLatestQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => Count.ToString();
}
public record BlogPostPagesLatestQueryResponse(IReadOnlyList<BlogPostPage> Items);
public class BlogPostPagesLatestQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<BlogPostPagesLatestQuery, BlogPostPagesLatestQueryResponse>(tools)
{
    public override async Task<BlogPostPagesLatestQueryResponse> Handle(BlogPostPagesLatestQuery request, CancellationToken cancellationToken = default)
    {
        // Optimized query to find content item identifiers
        var idsQuery = new ContentItemQueryBuilder().ForContentType(BlogPostContent.CONTENT_TYPE_NAME, queryParameters =>
        {
            _ = queryParameters
                .OrderBy(new[] { new OrderByColumn(nameof(BlogPostContent.BlogPostContentPublishedDate), OrderDirection.Descending) })
                .TopN(request.Count)
                .Columns(nameof(BlogPostContent.SystemFields.ContentItemID));
        });

        var contentItemIDs = (await Executor.GetResult(idsQuery, c => c.ContentItemID, DefaultQueryOptions, cancellationToken)).ToList();

        if (contentItemIDs.Count == 0)
        {
            return new([]);
        }

        // Full query to retrieve entire content graph
        var postsQuery = new ContentItemQueryBuilder().ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParams =>
        {
            _ = queryParams
                .ForWebsite(request.ChannelName)
                .Linking(nameof(BlogPostPage.BlogPostPageBlogPostContent), contentItemIDs)
                .WithLinkedItems(2);
        });

        var pages = await Executor.GetMappedWebPageResult<BlogPostPage>(postsQuery, DefaultQueryOptions, cancellationToken);

        return new([.. pages.OrderBy(p => contentItemIDs.IndexOf(p.BlogPostPageBlogPostContent.FirstOrDefault()?.SystemFields.ContentItemID ?? 0))]);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostPagesLatestQuery query, BlogPostPagesLatestQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            // Changes in post publish date for any post should invalidate this cache
            .AllContentItems(BlogPostContent.CONTENT_TYPE_NAME)
            .Collection(
                result.Items,
                (page, builder) => builder
                    // Only depend on pages referencing these posts
                    .WebPage(page.SystemFields.WebPageItemID)
                    // Add related content dependencies
                    .Collection(
                        page.BlogPostPageBlogPostContent.SelectMany(c => c.BlogPostContentAuthor),
                        (author, builder) => builder
                            .ContentItem(author)
                            .Collection(
                                author.AuthorContentPhotoMediaFileImage,
                                (image, builder) => builder.Media(image))));
}
