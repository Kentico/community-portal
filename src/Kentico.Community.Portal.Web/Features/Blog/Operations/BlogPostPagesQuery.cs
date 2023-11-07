using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostPagesByWebPageGUIDQuery(Guid[] WebPageGUIDs) : IQuery<BlogPostPagesQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => string.Join(",", WebPageGUIDs);
}
public record BlogPostPagesQueryResponse(IReadOnlyList<BlogPostPage> Items);
public class BlogPostPagesByWebPageGUIDQueryHandler : ContentItemQueryHandler<BlogPostPagesByWebPageGUIDQuery, BlogPostPagesQueryResponse>
{
    public BlogPostPagesByWebPageGUIDQueryHandler(ContentItemQueryTools tools) : base(tools) { }

    public override async Task<BlogPostPagesQueryResponse> Handle(BlogPostPagesByWebPageGUIDQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParameters =>
        {
            _ = queryParameters
                .ForWebsite(WebsiteChannelContextContext.WebsiteChannelName)
                .Where(w => w.WhereIn(nameof(WebPageFields.WebPageItemGUID), request.WebPageGUIDs))
                .WithLinkedItems(2);
        });

        var pages = await Executor.GetWebPageResult(b, WebPageMapper.Map<BlogPostPage>, DefaultQueryOptions, cancellationToken);

        return new(pages.OrderBy(p => Array.IndexOf(request.WebPageGUIDs, p.SystemFields.WebPageItemGUID)).ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostPagesByWebPageGUIDQuery query, BlogPostPagesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Collection(
            result.Items,
            (page, builder) => builder
                .WebPage(page)
                .Collection(
                    page.BlogPostPageBlogPostContent,
                    (content, builder) => builder
                        .ContentItem(content)
                        .Collection(
                            content.BlogPostContentAuthor,
                            (author, builder) => builder.ContentItem(author)
                                .Collection(
                                    author.AuthorContentPhotoMediaFileImage,
                                    (image, builder) => builder.Media(image)))));
}

public record BlogPostPagesByWebPageIDQuery(int[] WebPageIDs) : IQuery<BlogPostPagesQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => string.Join(",", WebPageIDs);
}
public class BlogPostPagesByWebPageIDQueryHandler : ContentItemQueryHandler<BlogPostPagesByWebPageIDQuery, BlogPostPagesQueryResponse>
{
    public BlogPostPagesByWebPageIDQueryHandler(ContentItemQueryTools tools) : base(tools) { }

    public override async Task<BlogPostPagesQueryResponse> Handle(BlogPostPagesByWebPageIDQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParameters =>
        {
            _ = queryParameters
                .ForWebsite(WebsiteChannelContextContext.WebsiteChannelName)
                .Where(w => w.WhereIn(nameof(WebPageFields.WebPageItemID), request.WebPageIDs))
                .WithLinkedItems(2);
        });

        var pages = await Executor.GetWebPageResult(b, WebPageMapper.Map<BlogPostPage>, DefaultQueryOptions, cancellationToken);

        return new(pages.ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostPagesByWebPageIDQuery query, BlogPostPagesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Collection(
            result.Items,
            (page, builder) => builder
                .WebPage(page)
                .Collection(
                    page.BlogPostPageBlogPostContent,
                    (content, builder) => builder
                        .ContentItem(content)
                        .Collection(
                            content.BlogPostContentAuthor,
                            (author, builder) => builder.ContentItem(author)
                                .Collection(
                                    author.AuthorContentPhotoMediaFileImage,
                                    (image, builder) => builder.Media(image)))));
}
