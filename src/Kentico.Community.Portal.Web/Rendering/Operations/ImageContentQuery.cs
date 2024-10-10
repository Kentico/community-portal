using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Rendering;

public record ImageContentQuery(int ContentItemID) : IQuery<Maybe<ImageContent>>, ICacheByValueQuery
{
    public string CacheValueKey => ContentItemID.ToString();
}

public class ImageContentQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<ImageContentQuery, Maybe<ImageContent>>(tools)
{
    public override async Task<Maybe<ImageContent>> Handle(ImageContentQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                ImageContent.CONTENT_TYPE_NAME,
                q => q.ForContentItem(request.ContentItemID));

        return await Executor.GetMappedResult<ImageContent>(b, DefaultQueryOptions, cancellationToken)
            .TryFirst();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(ImageContentQuery query, Maybe<ImageContent> result, ICacheDependencyKeysBuilder builder) =>
        builder.ContentItem(query.ContentItemID);
}

public record ImageContentByGUIDQuery(Guid ContentItemGUID) : IQuery<Maybe<ImageContent>>, ICacheByValueQuery
{
    public string CacheValueKey => ContentItemGUID.ToString();
}

public class ImageContentByGUIDQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<ImageContentByGUIDQuery, Maybe<ImageContent>>(tools)
{
    public override async Task<Maybe<ImageContent>> Handle(ImageContentByGUIDQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                ImageContent.CONTENT_TYPE_NAME,
                q => q.ForContentItem(request.ContentItemGUID));

        return await Executor.GetMappedResult<ImageContent>(b, DefaultQueryOptions, cancellationToken)
            .TryFirst();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(ImageContentByGUIDQuery query, Maybe<ImageContent> result, ICacheDependencyKeysBuilder builder) =>
        builder.ContentItem(query.ContentItemGUID);
}
