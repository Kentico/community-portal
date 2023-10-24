using CMS.DataEngine;
using CMS.FormEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record BlogPostTaxonomiesQuery() : IQuery<BlogPostTaxonomiesQueryResponse>;

public record BlogPostTaxonomy(string Value, string DisplayName);
public record BlogPostTaxonomiesQueryResponse(IReadOnlyList<BlogPostTaxonomy> Items);
public class BlogPostTaxonomiesQueryHandler : ContentItemQueryHandler<BlogPostTaxonomiesQuery, BlogPostTaxonomiesQueryResponse>
{
    public BlogPostTaxonomiesQueryHandler(ContentItemQueryTools tools) : base(tools) { }

    public override Task<BlogPostTaxonomiesQueryResponse> Handle(BlogPostTaxonomiesQuery request, CancellationToken cancellationToken = default)
    {
        var dc = DataClassInfoProvider.GetDataClassInfo(BlogPostPage.CONTENT_TYPE_NAME);

        var form = new FormInfo(dc.ClassFormDefinition);

        var field = form.GetFormField(nameof(BlogPostPage.BlogPostPageTaxonomy));

        if (!field.Settings.ContainsKey("Options") || field.Settings["Options"] is not string options)
        {
            return Task.FromResult(new BlogPostTaxonomiesQueryResponse(new List<BlogPostTaxonomy>()));
        }

        var taxonomies = options
            .Split("\n")
            .Select(o => o.Split(";"))
            .Where(kv => kv.Length is > 0 and <= 2)
            .Select(kv => kv switch
            {
            [var key] => new BlogPostTaxonomy(key, key),
            [var key, var value] => new BlogPostTaxonomy(key, value),
                _ => throw new ArgumentException("Invalid number of elements"),
            })
            .ToList();

        return Task.FromResult(new BlogPostTaxonomiesQueryResponse(taxonomies));
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(BlogPostTaxonomiesQuery query, BlogPostTaxonomiesQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(DataClassInfo.OBJECT_TYPE);
}
