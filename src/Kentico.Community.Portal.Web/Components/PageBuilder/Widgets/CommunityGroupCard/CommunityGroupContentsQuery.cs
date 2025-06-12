using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityGroupCard;

public record CommunityGroupContentsQuery : IQuery<CommunityGroupContentsQueryResponse>;
public record CommunityGroupContentsQueryResponse(IReadOnlyList<CommunityGroupContent> Items);
public class CommunityGroupContentsQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<CommunityGroupContentsQuery, CommunityGroupContentsQueryResponse>(tools)
{
    public override async Task<CommunityGroupContentsQueryResponse> Handle(CommunityGroupContentsQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(CommunityGroupContent.CONTENT_TYPE_NAME, queryParameters =>
                queryParameters
                    .OrderBy(new OrderByColumn(nameof(CommunityGroupContent.ListableItemTitle), OrderDirection.Ascending))
                    .WithLinkedItems(2));

        var result = await Executor.GetMappedResult<CommunityGroupContent>(b, DefaultQueryOptions, cancellationToken);

        return new([.. result]);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(CommunityGroupContentsQuery query, CommunityGroupContentsQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            .AllContentItems(CommunityGroupContent.CONTENT_TYPE_NAME)
            .Collection(result.Items, (i, b) => b.ContentItem(i.FeaturedImageImageContent.FirstOrDefault()));
}

public record CommunityGroupContentByGUIDQuery(Guid ContentItemGUID) : IQuery<Maybe<CommunityGroupContent>>, ICacheByValueQuery
{
    public string CacheValueKey => ContentItemGUID.ToString();
}

public class CommunityGroupContentByGUIDQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<CommunityGroupContentByGUIDQuery, Maybe<CommunityGroupContent>>(tools)
{
    public override async Task<Maybe<CommunityGroupContent>> Handle(CommunityGroupContentByGUIDQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(CommunityGroupContent.CONTENT_TYPE_NAME, queryParameters =>
                queryParameters
                    .Where(w => w.WhereContentItem(request.ContentItemGUID))
                    .OrderBy(new OrderByColumn(nameof(CommunityGroupContent.ListableItemTitle), OrderDirection.Ascending))
                    .WithLinkedItems(2));

        var contents = await Executor.GetMappedResult<CommunityGroupContent>(b, DefaultQueryOptions, cancellationToken);

        return contents.FirstOrDefault();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(CommunityGroupContentByGUIDQuery query, Maybe<CommunityGroupContent> result, ICacheDependencyKeysBuilder builder) =>
        builder
            .ContentItem(result.Map(r => r.SystemFields.ContentItemID))
            .ContentItem(result.Bind(r => r.FeaturedImageImageContent.TryFirst()).Map(i => i.SystemFields.ContentItemID));
}

