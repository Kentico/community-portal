using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostsByTaxonomyQuery(Guid[] TagIdentifiers, int Limit, string ChannelName) : IQuery<BlogPostsByTaxonomyQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => $"{string.Join("|", TagIdentifiers)}|{Limit}";
}

public record BlogPostsByTaxonomyQueryResponse(IReadOnlyList<BlogPostPage> Items);
public class BlogPostsByTaxonomyQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<BlogPostsByTaxonomyQuery, BlogPostsByTaxonomyQueryResponse>(tools)
{
    public override async Task<BlogPostsByTaxonomyQueryResponse> Handle(BlogPostsByTaxonomyQuery request, CancellationToken cancellationToken = default)
    {
        // Optimized query to return only content item identifiers
        var contentsQuery = new ContentItemQueryBuilder().ForContentType(BlogPostContent.CONTENT_TYPE_NAME, queryParams =>
        {
            _ = queryParams
                .Where(w => w.WhereContainsTags(nameof(BlogPostContent.BlogPostContentBlogType), request.TagIdentifiers))
                .OrderBy(new[] { new OrderByColumn(nameof(BlogPostContent.BlogPostContentPublishedDate), OrderDirection.Descending) })
                .TopN(request.Limit)
                .Columns(nameof(BlogPostContent.SystemFields.ContentItemID));
        });

        var contentItemIDs = (await Executor.GetResult(contentsQuery, c => c.ContentItemID, DefaultQueryOptions, cancellationToken)).ToList();

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
                .WithLinkedItems(3);
        });

        var pages = await Executor.GetMappedWebPageResult<BlogPostPage>(postsQuery, DefaultQueryOptions, cancellationToken);

        return new([.. pages.OrderBy(p => contentItemIDs.IndexOf(p.BlogPostPageBlogPostContent.FirstOrDefault()?.SystemFields.ContentItemID ?? 0))]);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostsByTaxonomyQuery query, BlogPostsByTaxonomyQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            // Changes in post taxonomy for any post should invalidate this cache
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
                                author.AuthorContentPhotoImageContent,
                                (image, builder) => builder.ContentItem(image)
                            )));
}
