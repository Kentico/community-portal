using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Components.Widgets.Video;

public record VideoContentByGUIDQuery(Guid ContentItemGUID) : IQuery<Maybe<VideoContent>>, ICacheByValueQuery
{
    public string CacheValueKey => ContentItemGUID.ToString();
}

public class VideoContentByGUIDQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<VideoContentByGUIDQuery, Maybe<VideoContent>>(tools)
{
    public override async Task<Maybe<VideoContent>> Handle(VideoContentByGUIDQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                VideoContent.CONTENT_TYPE_NAME,
                queryParameters => queryParameters
                    .Where(w => w.WhereEquals(nameof(ContentItemFields.ContentItemGUID), request.ContentItemGUID))
                    .WithLinkedItems(1));

        var r = await Executor.GetMappedResult<VideoContent>(b, DefaultQueryOptions, cancellationToken);

        return r.FirstOrDefault() is { } video
            ? video
            : Maybe.None;
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(VideoContentByGUIDQuery query, Maybe<VideoContent> result, ICacheDependencyKeysBuilder builder) =>
        builder.ContentItem(query.ContentItemGUID);
}

