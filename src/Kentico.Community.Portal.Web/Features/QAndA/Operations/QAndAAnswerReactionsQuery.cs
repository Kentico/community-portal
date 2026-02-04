using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record AnswerReactionsData(
    int TotalCount,
    bool CurrentMemberHasReacted,
    List<string> MembersWhoReacted);

public record QAndAAnswerReactionsQuery(
    int AnswerId,
    int? CurrentMemberId = null)
    : IQuery<AnswerReactionsData>, ICacheByValueQuery
{
    public string CacheValueKey => $"{AnswerId}|{CurrentMemberId}";
}

public class QAndAAnswerReactionsQueryHandler(
    DataItemQueryTools tools,
    IInfoProvider<QAndAAnswerReactionInfo> reactionProvider,
    IInfoProvider<MemberInfo> memberProvider)
    : DataItemQueryHandler<QAndAAnswerReactionsQuery, AnswerReactionsData>(tools)
{
    private readonly IInfoProvider<QAndAAnswerReactionInfo> reactionProvider = reactionProvider;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;

    public override async Task<AnswerReactionsData> Handle(QAndAAnswerReactionsQuery request, CancellationToken cancellationToken = default)
    {
        // Get all upvote reactions for this answer
        var reactions = await reactionProvider.Get()
            .WhereEquals(nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionAnswerID), request.AnswerId)
            .OrderBy(nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionID))
            .GetEnumerableTypedResultAsync();

        int totalCount = reactions.Count();
        bool currentMemberHasReacted = request.CurrentMemberId.HasValue
            && reactions.Any(r => r.QAndAAnswerReactionMemberID == request.CurrentMemberId);

        var members = (await memberProvider.Get()
            .WhereIn(nameof(MemberInfo.MemberID), reactions.Select(r => r.QAndAAnswerReactionMemberID).Distinct())
            .GetEnumerableTypedResultAsync())
            .Select(CommunityMember.FromMemberInfo)
            .ToDictionary(m => m.Id);

        var memberDisplayNames = reactions
            .Select(r => r.QAndAAnswerReactionMemberID)
            .Distinct()
            .Select(mId => members.TryGetValue(mId, out var m) ? m.DisplayName : "")
            .ToList();

        return new AnswerReactionsData(
            totalCount,
            currentMemberHasReacted,
            memberDisplayNames);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndAAnswerReactionsQuery query, AnswerReactionsData result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(QAndAAnswerReactionInfo.OBJECT_TYPE);
}
