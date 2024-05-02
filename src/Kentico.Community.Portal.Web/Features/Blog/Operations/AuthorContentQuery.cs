using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Blog;

public record AuthorContentQuery(string AuthorCodeName) : IQuery<AuthorContentQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => AuthorCodeName;
}

public record AuthorContentQueryResponse(AuthorContent? Author);
public class AuthorContentQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<AuthorContentQuery, AuthorContentQueryResponse>(tools)
{
    public override async Task<AuthorContentQueryResponse> Handle(AuthorContentQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(AuthorContent.CONTENT_TYPE_NAME, queryParams =>
        {
            _ = queryParams.Where(w => w.WhereEquals(nameof(AuthorContent.AuthorContentCodeName), request.AuthorCodeName));
        });

        var r = await Executor.GetMappedResult<AuthorContent>(b, DefaultQueryOptions, cancellationToken);

        return new(r.FirstOrDefault());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(AuthorContentQuery query, AuthorContentQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.ContentItem(result.Author)
            .Collection(
                result.Author?.AuthorContentPhotoMediaFileImage,
                (image, builder) => builder.Media(image));
}
