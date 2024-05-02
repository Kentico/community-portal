using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Community;

public record CommunityGroupContentsQuery : IQuery<CommunityGroupContentsQueryResponse>;
public record CommunityGroupContentsQueryResponse(IReadOnlyList<CommunityGroupContent> Items);
public class CommunityGroupContentsQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<CommunityGroupContentsQuery, CommunityGroupContentsQueryResponse>(tools)
{
    public override async Task<CommunityGroupContentsQueryResponse> Handle(CommunityGroupContentsQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(CommunityGroupContent.CONTENT_TYPE_NAME, queryParameters =>
                queryParameters
                    .OrderBy(new OrderByColumn(nameof(CommunityGroupContent.CommunityGroupContentTitle), OrderDirection.Ascending))
                    .WithLinkedItems(1));

        var r = await Executor.GetMappedResult<CommunityGroupContent>(b, DefaultQueryOptions, cancellationToken);

        return new(r.ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(CommunityGroupContentsQuery query, CommunityGroupContentsQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            .AllContentItems(CommunityGroupContent.CONTENT_TYPE_NAME)
            .Collection(result.Items, (i, b) => b.ContentItem(i.CommunityGroupContentPrimaryImageMediaAssets.FirstOrDefault()));
}
