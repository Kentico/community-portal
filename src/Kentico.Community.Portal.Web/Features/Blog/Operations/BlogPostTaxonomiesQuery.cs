using System.Text.RegularExpressions;
using CMS.ContentEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostTaxonomiesQuery() : IQuery<BlogPostTaxonomiesQueryResponse>;

public class BlogPostTaxonomy
{
    public Guid Guid { get; set; }
    public string Name { get; set; }
    public string NormalizedName { get; set; }
    public string DisplayName { get; set; }

    public BlogPostTaxonomy(Tag tag)
    {
        Guid = tag.Identifier;
        Name = tag.Name;
        NormalizedName = RegexTools.AlphanumericRegex().Replace(tag.Name, "").ToLowerInvariant();
        DisplayName = tag.Title;
    }
}

public record BlogPostTaxonomiesQueryResponse(IReadOnlyList<BlogPostTaxonomy> Types, IReadOnlyList<BlogPostTaxonomy> DXTopics);
public class BlogPostTaxonomiesQueryHandler(DataItemQueryTools tools, ITaxonomyRetriever taxonomyRetriever) : DataItemQueryHandler<BlogPostTaxonomiesQuery, BlogPostTaxonomiesQueryResponse>(tools)
{
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;

    public override async Task<BlogPostTaxonomiesQueryResponse> Handle(BlogPostTaxonomiesQuery request, CancellationToken cancellationToken = default)
    {
        var typeTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.BlogTypeTaxonomy.CodeName, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        var typeTags = typeTaxonomy.Tags
            .Select(tag => new BlogPostTaxonomy(tag))
            .ToList();

        var topicsTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(SystemTaxonomies.DXTopicTaxonomy.CodeName, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);
        var topicTags = topicsTaxonomy.Tags
            .Select(tag => new BlogPostTaxonomy(tag))
            .ToList();

        return new BlogPostTaxonomiesQueryResponse(typeTags, topicTags);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostTaxonomiesQuery query, BlogPostTaxonomiesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Object(TaxonomyInfo.OBJECT_TYPE, SystemTaxonomies.BlogTypeTaxonomy.CodeName)
            .Collection(result.Types, i => builder.Object(TagInfo.OBJECT_TYPE, i.Name));
}

public partial class RegexTools
{
    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    public static partial Regex AlphanumericRegex();
}
