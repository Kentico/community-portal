using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.Integrations;

public record IntegrationContentsQuery : IQuery<IntegrationContentsQueryResponse>;
public record IntegrationContentsQueryResponse(IReadOnlyList<IntegrationContentAggregate> Items);
public class IntegrationContentAggregate
{
    public IntegrationContent Content { get; }
    public Maybe<CommunityMember> IntegrationAuthor { get; }

    public IntegrationContentAggregate(IntegrationContent content, Dictionary<int, CommunityMember> members)
    {
        Content = content;
        IntegrationAuthor = content.IntegrationContentHasMemberAuthor
            ? members.GetValueOrDefault(content.IntegrationContentAuthorMemberID) is CommunityMember member
                ? member
                : Maybe<CommunityMember>.None
            : Maybe<CommunityMember>.None;
    }

    public void Deconstruct(out IntegrationContent content, out Maybe<CommunityMember> integrationAuthor)
    {
        content = Content;
        integrationAuthor = IntegrationAuthor;
    }
}
public class IntegrationContentsQueryHandler(ContentItemQueryTools tools, IInfoProvider<MemberInfo> memberProvider) : ContentItemQueryHandler<IntegrationContentsQuery, IntegrationContentsQueryResponse>(tools)
{
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;

    public override async Task<IntegrationContentsQueryResponse> Handle(IntegrationContentsQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(
            IntegrationContent.CONTENT_TYPE_NAME,
            q => q
                .OrderBy(new OrderByColumn(nameof(IntegrationContent.IntegrationContentPublishedDate), OrderDirection.Descending))
                .WithLinkedItems(1));

        var contents = await Executor.GetMappedWebPageResult<IntegrationContent>(b, DefaultQueryOptions, cancellationToken);
        var members = await GetIntegrationAuthors(contents);

        var items = contents
            .Select(c => new IntegrationContentAggregate(c, members))
            .ToList();

        return new(items);
    }

    private async Task<Dictionary<int, CommunityMember>> GetIntegrationAuthors(IEnumerable<IntegrationContent> contents)
    {
        var memberAuthorIDs = contents
            .Select(c => c.IntegrationContentAuthorMemberID)
            .Where(id => id > 0)
            .Distinct();

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
