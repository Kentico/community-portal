using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostPagesByWebPageGUIDQuery(Guid[] WebPageGUIDs, string ChannelName) : IQuery<BlogPostPagesQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => string.Join(",", WebPageGUIDs);
}
public record BlogPostPagesQueryResponse(IReadOnlyList<BlogPostPage> Items);
public class BlogPostPagesByWebPageGUIDQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<BlogPostPagesByWebPageGUIDQuery, BlogPostPagesQueryResponse>(tools)
{
    public override async Task<BlogPostPagesQueryResponse> Handle(BlogPostPagesByWebPageGUIDQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParameters =>
        {
            _ = queryParameters
                .ForWebsite(request.ChannelName)
                .Where(w => w.WhereIn(nameof(WebPageFields.WebPageItemGUID), request.WebPageGUIDs))
                .WithLinkedItems(BlogPostPage.FullQueryDepth);
        });

        var pages = await Executor.GetMappedWebPageResult<BlogPostPage>(b, DefaultQueryOptions, cancellationToken);

        return new([.. pages.OrderBy(p => Array.IndexOf(request.WebPageGUIDs, p.SystemFields.WebPageItemGUID))]);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostPagesByWebPageGUIDQuery query, BlogPostPagesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Collection(
            result.Items,
            (page, builder) => builder
                .WebPage(page)
                .Collection(
                    page.BlogPostPageAuthorContent,
                    (author, builder) => builder.ContentItem(author)
                        .Collection(
                            author.AuthorContentPhotoImageContent,
                            (image, builder) => builder.ContentItem(image)
                        )));
}

public record BlogPostPagesByWebPageIDQuery(int[] WebPageIDs, string ChannelName) : IQuery<BlogPostPagesQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => string.Join(",", WebPageIDs);
}
public class BlogPostPagesByWebPageIDQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<BlogPostPagesByWebPageIDQuery, BlogPostPagesQueryResponse>(tools)
{
    public override async Task<BlogPostPagesQueryResponse> Handle(BlogPostPagesByWebPageIDQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParameters =>
        {
            _ = queryParameters
                .ForWebsite(request.ChannelName)
                .Where(w => w.WhereIn(nameof(WebPageFields.WebPageItemID), request.WebPageIDs))
                .WithLinkedItems(3);
        });

        var pages = await Executor.GetMappedWebPageResult<BlogPostPage>(b, DefaultQueryOptions, cancellationToken);

        return new(pages.ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostPagesByWebPageIDQuery query, BlogPostPagesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Collection(
            result.Items,
            (page, builder) => builder
                .WebPage(page)
                .Collection(
                    page.BlogPostPageAuthorContent,
                    (author, builder) => builder.ContentItem(author)
                        .Collection(
                            author.AuthorContentPhotoImageContent,
                            (image, builder) => builder.ContentItem(image)
                        )));
}
