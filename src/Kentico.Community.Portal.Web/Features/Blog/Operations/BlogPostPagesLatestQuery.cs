using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostPagesLatestQuery(int Count) : IQuery<BlogPostPagesLatestQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => Count.ToString();
}
public record BlogPostPagesLatestQueryResponse(IReadOnlyList<BlogPostPage> Items);
public class BlogPostPagesLatestQueryHandler : ContentItemQueryHandler<BlogPostPagesLatestQuery, BlogPostPagesLatestQueryResponse>
{
    public BlogPostPagesLatestQueryHandler(ContentItemQueryTools tools) : base(tools) { }

    public override async Task<BlogPostPagesLatestQueryResponse> Handle(BlogPostPagesLatestQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParameters =>
        {
            _ = queryParameters
                .OrderBy(new[] { new OrderByColumn(nameof(BlogPostPage.BlogPostPageDate), OrderDirection.Descending) })
                .TopN(request.Count)
                .WithLinkedItems(1);
        });

        var pages = await Executor.GetWebPageResult(b, WebPageMapper.Map<BlogPostPage>, DefaultQueryOptions, cancellationToken);

        return new(pages.ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostPagesLatestQuery query, BlogPostPagesLatestQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllContentItems(BlogPostPage.CONTENT_TYPE_NAME);
}
