using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Integrations;

public record IntegrationContentsByMemberIDQuery(int MemberID) : IQuery<IntegrationContentsByMemberIDQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => MemberID.ToString();
}

public record IntegrationContentsByMemberIDQueryResponse(IReadOnlyList<IntegrationContent> Items);
public class IntegrationContentsByMemberIDQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<IntegrationContentsByMemberIDQuery, IntegrationContentsByMemberIDQueryResponse>(tools)
{
    public override async Task<IntegrationContentsByMemberIDQueryResponse> Handle(IntegrationContentsByMemberIDQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(
            IntegrationContent.CONTENT_TYPE_NAME,
            q => q
                .Where(w => w
                    .WhereEquals(nameof(IntegrationContent.IntegrationContentAuthorMemberID), request.MemberID)
                    .WhereTrue(nameof(IntegrationContent.IntegrationContentHasMemberAuthor)))
                .OrderBy(new OrderByColumn(nameof(IntegrationContent.IntegrationContentPublishedDate), OrderDirection.Descending)));

        var contents = await Executor.GetMappedWebPageResult<IntegrationContent>(b, DefaultQueryOptions, cancellationToken);

        return new([.. contents]);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(IntegrationContentsByMemberIDQuery query, IntegrationContentsByMemberIDQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            .AllContentItems(IntegrationContent.CONTENT_TYPE_NAME);
}
