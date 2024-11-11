using System.Text.RegularExpressions;
using CMS.ContentEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndATaxonomiesQuery() : IQuery<QAndATaxonomiesQueryResponse>;

public class QAndATaxonomy
{
    public Guid Guid { get; set; }
    public string Name { get; set; }
    public string NormalizedName { get; set; }
    public string DisplayName { get; set; }

    public QAndATaxonomy(Tag tag)
    {
        Guid = tag.Identifier;
        Name = tag.Name;
        NormalizedName = RegexTools.AlphanumericRegex().Replace(tag.Name, "").ToLowerInvariant();
        DisplayName = tag.Title;
    }
}

public record QAndATaxonomiesQueryResponse(IReadOnlyList<QAndATaxonomy> Types, IReadOnlyList<QAndATaxonomy> DXTopics);
public class QAndATaxonomiesQueryHandler(DataItemQueryTools tools, ITaxonomyRetriever taxonomyRetriever) : DataItemQueryHandler<QAndATaxonomiesQuery, QAndATaxonomiesQueryResponse>(tools)
{
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;

    public override async Task<QAndATaxonomiesQueryResponse> Handle(QAndATaxonomiesQuery request, CancellationToken cancellationToken = default)
    {
        var typeTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.QAndADiscussionTypeTaxonomy.CodeName, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        var typeTags = typeTaxonomy.Tags
            .Select(tag => new QAndATaxonomy(tag))
            .ToList();

        var topicsTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.DXTopicTaxonomy.CodeName, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        var topicTags = topicsTaxonomy.Tags
            .Select(tag => new QAndATaxonomy(tag))
            .ToList();

        return new QAndATaxonomiesQueryResponse(typeTags, topicTags);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndATaxonomiesQuery query, QAndATaxonomiesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Object(TaxonomyInfo.OBJECT_TYPE, SystemTaxonomies.BlogTypeTaxonomy.CodeName)
            .Collection(result.Types, i => builder.Object(TagInfo.OBJECT_TYPE, i.Name));
}

public partial class RegexTools
{
    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    public static partial Regex AlphanumericRegex();
}
