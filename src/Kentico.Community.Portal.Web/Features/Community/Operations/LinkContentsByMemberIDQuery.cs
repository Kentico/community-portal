using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Community;

public record LinkContentsByMemberIDQuery(int MemberID) : IQuery<LinkContentsByMemberIDQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => MemberID.ToString();
}
public record LinkContentsByMemberIDQueryResponse(IReadOnlyList<LinkContent> Items);
public class LinkContentsByMemberIDQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<LinkContentsByMemberIDQuery, LinkContentsByMemberIDQueryResponse>(tools)
{
    public override async Task<LinkContentsByMemberIDQueryResponse> Handle(LinkContentsByMemberIDQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                LinkContent.CONTENT_TYPE_NAME,
                q => q
                    .Where(w => w.WhereEquals(
                        nameof(LinkContent.LinkContentMemberID), request.MemberID))
                     .OrderBy(new OrderByColumn(nameof(LinkContent.LinkContentPublishedDate), OrderDirection.Descending)));

        var contents = await Executor.GetMappedResult<LinkContent>(b, DefaultQueryOptions, cancellationToken);

        return new([.. contents]);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(LinkContentsByMemberIDQuery query, LinkContentsByMemberIDQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Collection(result.Items, (l, b) => b.ContentItem(l.SystemFields.ContentItemID));
}
