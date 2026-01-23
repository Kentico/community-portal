using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostsBySmartFolderQuery(Guid FolderIdentifier, int Limit, string ChannelName) : IQuery<BlogPostsBySmartFolderQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => $"{FolderIdentifier}|{Limit}";
}

public record BlogPostsBySmartFolderQueryResponse(IReadOnlyList<BlogPostPage> Items);
public class BlogPostsBySmartFolderQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<BlogPostsBySmartFolderQuery, BlogPostsBySmartFolderQueryResponse>(tools)
{
    public override async Task<BlogPostsBySmartFolderQueryResponse> Handle(BlogPostsBySmartFolderQuery request, CancellationToken cancellationToken = default)
    {
        // Optimized query to return only content item identifiers
        var contentsQuery = new ContentItemQueryBuilder()
            .ForContentTypes(q => q
                .OfContentType(BlogPostContent.CONTENT_TYPE_NAME)
                .InSmartFolder(request.FolderIdentifier)
                .WithContentTypeFields())
        .Parameters(q => q
            .Columns(nameof(BlogPostContent.SystemFields.ContentItemID)));

        var contentItemIDs = (await Executor.GetResult(contentsQuery, c => c.ContentItemID, DefaultQueryOptions, cancellationToken)).ToList();

        if (contentItemIDs.Count == 0)
        {
            return new([]);
        }

        // Full query to retrieve entire content graph
        var postsQuery = new ContentItemQueryBuilder()
            .ForContentType(
                BlogPostPage.CONTENT_TYPE_NAME,
                q => q
                .ForWebsite(request.ChannelName)
                .Linking(nameof(BlogPostPage.BlogPostPageBlogPostContent), contentItemIDs)
                .OrderBy([new OrderByColumn(nameof(BlogPostPage.BlogPostPagePublishedDate), OrderDirection.Descending)])
                .TopN(request.Limit)
                .WithLinkedItems(BlogPostPage.FullQueryDepth));

        var pages = await Executor.GetMappedWebPageResult<BlogPostPage>(postsQuery, DefaultQueryOptions, cancellationToken);

        return new([.. pages]);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostsBySmartFolderQuery query, BlogPostsBySmartFolderQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            // Changes in post SmartFolder for any post should invalidate this cache
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
