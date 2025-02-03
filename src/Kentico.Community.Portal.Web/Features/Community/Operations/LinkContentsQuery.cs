using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Community;

public record LinkContentsQuery(Guid[] ContentItemGUIDs) : IQuery<LinkContentsQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => string.Join(",", ContentItemGUIDs);
}

public record LinkContentsQueryResponse(IReadOnlyList<LinkContent> Items);
public class LinkContentsQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<LinkContentsQuery, LinkContentsQueryResponse>(tools)
{
    public override async Task<LinkContentsQueryResponse> Handle(LinkContentsQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                LinkContent.CONTENT_TYPE_NAME,
                q => q
                    .Where(w => w.WhereIn(
                        nameof(ContentItemFields.ContentItemGUID),
                        request.ContentItemGUIDs.ToArray())));

        var contents = (await Executor.GetMappedResult<LinkContent>(b, DefaultQueryOptions, cancellationToken))
            .OrderBy(p => Array.IndexOf(request.ContentItemGUIDs, p.SystemFields.ContentItemGUID))
            .ToList();

        return new(contents);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(LinkContentsQuery query, LinkContentsQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Collection(result.Items, (l, b) => b.ContentItem(l.SystemFields.ContentItemID));
}
