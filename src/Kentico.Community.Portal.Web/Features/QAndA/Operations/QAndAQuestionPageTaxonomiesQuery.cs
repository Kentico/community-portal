using CMS.ContentEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndATaxonomiesQuery() : IQuery<QAndATaxonomiesQueryResponse>;

public record QAndATaxonomy(Guid Guid, string Value, string DisplayName);
public record QAndATaxonomiesQueryResponse(IReadOnlyList<QAndATaxonomy> DiscussionTypes, IReadOnlyList<QAndATaxonomy> DXTopics);
public class QAndATaxonomiesQueryHandler(DataItemQueryTools tools, ITaxonomyRetriever taxonomyRetriever) : DataItemQueryHandler<QAndATaxonomiesQuery, QAndATaxonomiesQueryResponse>(tools)
{
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;

    public override async Task<QAndATaxonomiesQueryResponse> Handle(QAndATaxonomiesQuery request, CancellationToken cancellationToken = default)
    {
        var discussionTypeTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.QAndADiscussionTypeTaxonomy.CodeName, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        var discussionTypeTags = discussionTypeTaxonomy.Tags
            .Select(tag => new QAndATaxonomy(tag.Identifier, tag.Name, tag.Title))
            .ToList();

        var dxTopicTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.DXTopicTaxonomy.CodeName, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        var dxTopicTags = dxTopicTaxonomy.Tags
            .Select(tag => new QAndATaxonomy(tag.Identifier, tag.Name, tag.Title))
            .ToList();

        return new QAndATaxonomiesQueryResponse(discussionTypeTags, dxTopicTags);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndATaxonomiesQuery query, QAndATaxonomiesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            .Object(TaxonomyInfo.OBJECT_TYPE, SystemTaxonomies.QAndADiscussionTypeTaxonomy.CodeName)
            .Object(TaxonomyInfo.OBJECT_TYPE, SystemTaxonomies.DXTopicTaxonomy.CodeName)
            .Collection(result.DiscussionTypes, (i, b) => b.Object(TagInfo.OBJECT_TYPE, i.Value))
            .Collection(result.DXTopics, (i, b) => b.Object(TagInfo.OBJECT_TYPE, i.Value));
}
