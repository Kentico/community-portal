using System.Collections.Frozen;
using System.Collections.Immutable;
using CMS.ContentEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndATaxonomiesQuery() : IQuery<QAndATaxonomiesQueryResponse>;

public record QAndATaxonomiesQueryResponse(
    IReadOnlyList<TaxonomyTag> Types,
    IReadOnlyList<TaxonomyTag> DXTopicsHierarchy,
    IReadOnlyList<TaxonomyTag> DXTopicsAll);
public class QAndATaxonomiesQueryHandler(DataItemQueryTools tools, ITaxonomyRetriever taxonomyRetriever) : DataItemQueryHandler<QAndATaxonomiesQuery, QAndATaxonomiesQueryResponse>(tools)
{
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;

    public override async Task<QAndATaxonomiesQueryResponse> Handle(QAndATaxonomiesQuery request, CancellationToken cancellationToken = default)
    {
        var typeTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.QAndADiscussionTypeTaxonomy.CodeName, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        var childTypeTags = new Dictionary<int, ImmutableList<Tag>>().ToFrozenDictionary();
        var typeTags = typeTaxonomy.Tags
            .Select(tag => new TaxonomyTag(tag, childTypeTags))
            .ToList();

        var topicsTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.DXTopicTaxonomy.CodeName, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        var topicTagsByParentID = topicsTaxonomy
            .Tags
            .GroupBy(t => t.ParentID)
            .ToFrozenDictionary(b => b.Key, g => g.ToImmutableList());
        var topicTags = (topicsTaxonomy.Tags.Any(t => t.ParentID != 0)
                ? topicsTaxonomy.Tags.Where(t => t.ParentID == 0)
                : topicsTaxonomy.Tags)
            .OrderBy(t => t.Title)
            .Select(tag => new TaxonomyTag(tag, topicTagsByParentID))
            .ToList();
        var topicsAll = topicTags
            .SelectMany(tt => tt.Children)
            .ToList();

        return new QAndATaxonomiesQueryResponse(typeTags, topicTags, topicsAll);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndATaxonomiesQuery query, QAndATaxonomiesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Object(TaxonomyInfo.OBJECT_TYPE, SystemTaxonomies.DXTopicTaxonomy.CodeName)
            .Collection(result.Types, i => builder.Object(TagInfo.OBJECT_TYPE, i.Name));
}
