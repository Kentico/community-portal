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
        // Full query to retrieve entire content graph
        var postsQuery = new ContentItemQueryBuilder()
            .ForContentType(
                BlogPostPage.CONTENT_TYPE_NAME,
                q => q
                    .ForWebsite(request.ChannelName)
                    .Where(w => w.WhereContainsTags(nameof(BlogPostPage.BlogPostPageBlogType), request.TagIdentifiers))
                    .OrderBy([new OrderByColumn(nameof(BlogPostPage.BlogPostPagePublishedDate), OrderDirection.Descending)])
                    .TopN(request.Limit)
                    .WithLinkedItems(3));

        var pages = await Executor.GetMappedWebPageResult<BlogPostPage>(postsQuery, DefaultQueryOptions, cancellationToken);

        return new([.. pages]);
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
                        page.BlogPostPageAuthorContent,
                        (author, builder) => builder
                            .ContentItem(author)
                            .Collection(
                                author.AuthorContentPhotoImageContent,
                                (image, builder) => builder.ContentItem(image)
                            )));
}
