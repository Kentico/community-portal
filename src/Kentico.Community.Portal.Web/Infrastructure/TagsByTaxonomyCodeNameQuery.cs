using CMS.ContentEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Infrastructure;

public record TagsByTaxonomyCodeNameQuery(string TaxonomyCodeName) : IQuery<TagsByTaxonomyCodeNameQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => TaxonomyCodeName;
}

public record TagsByTaxonomyCodeNameQueryResponse(IReadOnlyList<Tag> Tags);

public class TagsByTaxonomyCodeNameQueryHandler(DataItemQueryTools tools, ITaxonomyRetriever taxonomyRetriever)
    : DataItemQueryHandler<TagsByTaxonomyCodeNameQuery, TagsByTaxonomyCodeNameQueryResponse>(tools)
{
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;

    public override async Task<TagsByTaxonomyCodeNameQueryResponse> Handle(TagsByTaxonomyCodeNameQuery request, CancellationToken cancellationToken = default)
    {
        var taxonomy = await taxonomyRetriever.RetrieveTaxonomy(
            request.TaxonomyCodeName,
            PortalWebSiteChannel.DEFAULT_LANGUAGE,
            cancellationToken);

        var tags = taxonomy.Tags
            .OrderBy(t => t.Title)
            .ToList();

        return new TagsByTaxonomyCodeNameQueryResponse(tags);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        TagsByTaxonomyCodeNameQuery query,
        TagsByTaxonomyCodeNameQueryResponse result,
        ICacheDependencyKeysBuilder builder) =>
        builder
            .Object(TaxonomyInfo.OBJECT_TYPE, query.TaxonomyCodeName)
            .Collection(result.Tags, t => builder.Object(TagInfo.OBJECT_TYPE, t.Identifier));
}
