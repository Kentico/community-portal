using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Rendering;

public record MediaAssetContentQuery(int ContentItemID) : IQuery<MediaAssetContent>, ICacheByValueQuery
{
    public string CacheValueKey => ContentItemID.ToString();
}

public class MediaAssetContentQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<MediaAssetContentQuery, MediaAssetContent>(tools)
{
    public override async Task<MediaAssetContent> Handle(MediaAssetContentQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                MediaAssetContent.CONTENT_TYPE_NAME,
                q => q.ForContentItem(request.ContentItemID));

        var r = await Executor.GetMappedResult<MediaAssetContent>(b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(MediaAssetContentQuery query, MediaAssetContent result, ICacheDependencyKeysBuilder builder) =>
        builder.ContentItem(query.ContentItemID);
}

public record MediaAssetContentByGUIDQuery(Guid ContentItemGUID) : IQuery<MediaAssetContent>, ICacheByValueQuery
{
    public string CacheValueKey => ContentItemGUID.ToString();
}

public class MediaAssetContentByGUIDQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<MediaAssetContentByGUIDQuery, MediaAssetContent>(tools)
{
    public override async Task<MediaAssetContent> Handle(MediaAssetContentByGUIDQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                MediaAssetContent.CONTENT_TYPE_NAME,
                q => q.ForContentItem(request.ContentItemGUID));

        var r = await Executor.GetMappedResult<MediaAssetContent>(b, DefaultQueryOptions, cancellationToken);

        return r.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(MediaAssetContentByGUIDQuery query, MediaAssetContent result, ICacheDependencyKeysBuilder builder) =>
        builder.ContentItem(query.ContentItemGUID);
}
