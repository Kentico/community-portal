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
        var b = new ContentItemQueryBuilder().ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParams =>
        {
            _ = queryParams
                .ForWebsite(WebsiteChannelContext.WebsiteChannelName, pathMatch: null, true)
                .Where(w => w.WhereEquals(nameof(BlogPostPage.BlogPostPageTaxonomy), request.TaxonomyName))
                .TopN(request.Limit)
                .OrderBy(new[] { new OrderByColumn(nameof(BlogPostPage.BlogPostPageDate), OrderDirection.Descending) })
                .WithLinkedItems(1);
        });

        var pages = await Executor.GetWebPageResult(b, Mapper.Map<BlogPostPage>, DefaultQueryOptions, cancellationToken);

        return new(pages.ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostsByTaxonomyQuery query, BlogPostsByTaxonomyQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllContentItems(BlogPostPage.CONTENT_TYPE_NAME);
}
