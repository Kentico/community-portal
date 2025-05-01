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

        // Full query to retrieve entire content graph
        var postsQuery = new ContentItemQueryBuilder()
            .ForContentType(
                BlogPostPage.CONTENT_TYPE_NAME,
                q => q
                    .ForWebsite(request.ChannelName)
                    .OrderBy([new OrderByColumn(nameof(BlogPostPage.BlogPostPagePublishedDate), OrderDirection.Descending)])
                    .TopN(request.Count)
                    .WithLinkedItems(BlogPostPage.FullQueryDepth));

        var pages = await Executor.GetMappedWebPageResult<BlogPostPage>(postsQuery, DefaultQueryOptions, cancellationToken);

        return new([.. pages]);
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
                        page.BlogPostPageAuthorContent,
                        (author, builder) => builder
                            .ContentItem(author)
                            .Collection(
                                author.AuthorContentPhotoImageContent,
                                (image, builder) => builder.ContentItem(image)
                            )));
}
