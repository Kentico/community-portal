using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Rendering;

public record MediaAssetContentQuery(int ContentItemID) : IQuery<MediaAssetContent>;
public class MediaAssetContentQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<MediaAssetContentQuery, MediaAssetContent>(tools)
{
    public override async Task<MediaAssetContent> Handle(MediaAssetContentQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(MediaAssetContent.CONTENT_TYPE_NAME, queryParameters => queryParameters.Where(w => w.WhereEquals(nameof(ContentItemFields.ContentItemID), request.ContentItemID)));

        var r = await Executor.GetResult(b, ContentItemMapper.Map<MediaAssetContent>, DefaultQueryOptions, cancellationToken);

        return r.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(MediaAssetContentQuery query, MediaAssetContent result, ICacheDependencyKeysBuilder builder) =>
        builder.ContentItem(query.ContentItemID);
}
