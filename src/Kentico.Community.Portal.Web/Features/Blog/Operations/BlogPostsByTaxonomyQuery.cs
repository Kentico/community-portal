using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostsByTaxonomyQuery(string TaxonomyName, int Limit) : IQuery<BlogPostsByTaxonomyQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => $"{TaxonomyName}|{Limit}";
}
public record BlogPostsByTaxonomyQueryResponse(IReadOnlyList<BlogPostPage> Items);
public class BlogPostsByTaxonomyQueryHandler : WebPageQueryHandler<BlogPostsByTaxonomyQuery, BlogPostsByTaxonomyQueryResponse>
{
    public BlogPostsByTaxonomyQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<BlogPostsByTaxonomyQueryResponse> Handle(BlogPostsByTaxonomyQuery request, CancellationToken cancellationToken = default)
    {
        // Optimized query to find content item identifiers
        var identifiersQuery = new ContentItemQueryBuilder().ForContentType(BlogPostContent.CONTENT_TYPE_NAME, queryParams =>
        {
            _ = queryParams
                .Where(w => w.WhereEquals(nameof(BlogPostContent.BlogPostContentTaxonomy), request.TaxonomyName))
                .OrderBy(new[] { new OrderByColumn(nameof(BlogPostContent.BlogPostContentPublishedDate), OrderDirection.Descending) })
                .TopN(request.Limit)
                .Columns(nameof(BlogPostContent.SystemFields.ContentItemID));
        });

        var contentItemIDs = (await Executor.GetResult(identifiersQuery, c => c.ContentItemID, DefaultQueryOptions, cancellationToken)).ToList();

        if (contentItemIDs.Count == 0)
        {
            return new(new List<BlogPostPage>());
        }

        // Full query to retrieve entire content graph
        var postsQuery = new ContentItemQueryBuilder().ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParams =>
        {
            _ = queryParams
                .ForWebsite(WebsiteChannelContext.WebsiteChannelName)
                .Linking(nameof(BlogPostPage.BlogPostPageBlogPostContent), contentItemIDs)
                .WithLinkedItems(2);
        });

        var pages = await Executor.GetWebPageResult(postsQuery, WebPageMapper.Map<BlogPostPage>, DefaultQueryOptions, cancellationToken);

        return new(pages.ToList());
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
                                author.AuthorContentPhotoMediaFileImage,
                                (image, builder) => builder.Media(image))));
}
