using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using EnumsNET;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.Community;

public record LinkContentsCommunityQuery(int LinkPublishDelayDays) : IQuery<IReadOnlyList<LinkContentsCommunityQueryResponse>>, ICacheByValueQuery
{
    public string CacheValueKey => LinkPublishDelayDays.ToString();
}
public record LinkContentsCommunityQueryResponse(LinkContent Content, Maybe<CommunityMember> Member);
public class LinkContentsCommunityQueryHandler(
    ContentItemQueryTools tools,
    IInfoProvider<MemberInfo> memberProvider,
    TimeProvider time) : ContentItemQueryHandler<LinkContentsCommunityQuery, IReadOnlyList<LinkContentsCommunityQueryResponse>>(tools)
{
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly TimeProvider time = time;

    public override async Task<IReadOnlyList<LinkContentsCommunityQueryResponse>> Handle(LinkContentsCommunityQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(
                q => q
                    .OfContentType(LinkContent.CONTENT_TYPE_NAME)
                    .WithContentTypeFields())
            .Parameters(
                q => q
                    .Where(w => w
                        .WhereEquals(nameof(LinkContent.LinkContentLinkType), LinkContentLinkTypes.CommunityContribution.GetName())
                        .WhereLessOrEquals(nameof(LinkContent.LinkContentPublishedDate), time.GetLocalNow().AddDays(-request.LinkPublishDelayDays)))
                    .OrderBy(new OrderByColumn(nameof(LinkContent.LinkContentPublishedDate), OrderDirection.Descending)));

        var contents = await Executor.GetMappedResult<LinkContent>(b, DefaultQueryOptions, cancellationToken);

        var memberIDs = contents.Select(c => c.LinkContentMemberID);

        var members = (await memberProvider
            .Get()
            .WhereIn(nameof(MemberInfo.MemberID), memberIDs)
            .GetEnumerableTypedResultAsync())
            .ToDictionary(m => m.MemberID);

        return [.. contents.Select(c => new LinkContentsCommunityQueryResponse(c, members.TryFind(c.LinkContentMemberID).Map(m => m.AsCommunityMember())))];
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(LinkContentsCommunityQuery query, IReadOnlyList<LinkContentsCommunityQueryResponse> result, ICacheDependencyKeysBuilder builder) =>
        builder.AllContentItems(LinkContent.CONTENT_TYPE_NAME)
            .Collection(result, (i, b) => b.Object(MemberInfo.OBJECT_TYPE, i.Content.LinkContentMemberID));
}
