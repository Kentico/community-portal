using CMS.ContentEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostTaxonomiesQuery() : IQuery<BlogPostTaxonomiesQueryResponse>;

public record BlogPostTaxonomy(Guid Guid, string Value, string DisplayName);
public record BlogPostTaxonomiesQueryResponse(IReadOnlyList<BlogPostTaxonomy> Items);
public class BlogPostTaxonomiesQueryHandler(DataItemQueryTools tools, ITaxonomyRetriever taxonomyRetriever) : DataItemQueryHandler<BlogPostTaxonomiesQuery, BlogPostTaxonomiesQueryResponse>(tools)
{
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;

    public override async Task<BlogPostTaxonomiesQueryResponse> Handle(BlogPostTaxonomiesQuery request, CancellationToken cancellationToken = default)
    {
        var taxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.BlogTypeTaxonomy.CodeName, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);

        var taxonomies = taxonomy.Tags
            .Select(tag => new BlogPostTaxonomy(tag.Identifier, tag.Name, tag.Title))
            .ToList();

        return new BlogPostTaxonomiesQueryResponse(taxonomies);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostTaxonomiesQuery query, BlogPostTaxonomiesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Object(TaxonomyInfo.OBJECT_TYPE, SystemTaxonomies.BlogTypeTaxonomy.CodeName)
            .Collection(result.Items, i => builder.Object(TagInfo.OBJECT_TYPE, i.Value));
}
