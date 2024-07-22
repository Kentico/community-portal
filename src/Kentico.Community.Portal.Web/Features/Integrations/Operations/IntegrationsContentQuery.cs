using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.Integrations;

public record IntegrationContentsQuery : IQuery<IntegrationContentsQueryResponse>;
public record IntegrationContentsQueryResponse(IReadOnlyList<IntegrationContentAggregate> Items);
public record IntegrationContentAggregate(IntegrationContent Content, Maybe<CommunityMember> IntegrationAuthor);
public class IntegrationContentsQueryHandler(ContentItemQueryTools tools, IInfoProvider<MemberInfo> memberProvider) : ContentItemQueryHandler<IntegrationContentsQuery, IntegrationContentsQueryResponse>(tools)
{
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;

    public override async Task<IntegrationContentsQueryResponse> Handle(IntegrationContentsQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(
            IntegrationContent.CONTENT_TYPE_NAME,
            q => q.OrderBy(new OrderByColumn(nameof(IntegrationContent.IntegrationContentPublishedDate), OrderDirection.Descending)));

        var contents = await Executor.GetMappedWebPageResult<IntegrationContent>(b, DefaultQueryOptions, cancellationToken);

        var members = await GetIntegrationAuthors(contents);

        var items = contents
            .Select(c => new IntegrationContentAggregate(c, Maybe.From(members.GetValueOrDefault(c.IntegrationContentAuthorMemberID)!)))
            .ToList();

        return new(items);
    }

    private async Task<Dictionary<int, CommunityMember>> GetIntegrationAuthors(IEnumerable<IntegrationContent> contents)
    {
        var memberAuthorIDs = contents.Select(c => c.IntegrationContentAuthorMemberID).Distinct().ToList();

        var members = await memberProvider
                        .Get()
                        .WhereIn(nameof(MemberInfo.MemberID), memberAuthorIDs)
                        .GetEnumerableTypedResultAsync();

        return members.Select(CommunityMember.FromMemberInfo).ToDictionary(m => m.Id);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(IntegrationContentsQuery query, IntegrationContentsQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder
            .AllContentItems(IntegrationContent.CONTENT_TYPE_NAME)
            .Collection(result.Items, (m, b) => m.IntegrationAuthor.Execute(a => b.Object(MemberInfo.OBJECT_TYPE, a.Id)));
}
