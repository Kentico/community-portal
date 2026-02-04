using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QuestionReactionsData(
    int TotalCount,
    bool CurrentMemberHasReacted,
    List<string> MembersWhoReacted);

public record QAndAQuestionReactionsQuery(
    int QuestionWebPageItemId,
    int? CurrentMemberId = null)
    : IQuery<QuestionReactionsData>, ICacheByValueQuery
{
    public string CacheValueKey => $"{QuestionWebPageItemId}|{CurrentMemberId}";
}

public class QAndAQuestionReactionsQueryHandler(
    DataItemQueryTools tools,
    IInfoProvider<QAndAQuestionReactionInfo> reactionProvider,
    IInfoProvider<MemberInfo> memberProvider)
    : DataItemQueryHandler<QAndAQuestionReactionsQuery, QuestionReactionsData>(tools)
{
    private readonly IInfoProvider<QAndAQuestionReactionInfo> reactionProvider = reactionProvider;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;

    public override async Task<QuestionReactionsData> Handle(QAndAQuestionReactionsQuery request, CancellationToken cancellationToken = default)
    {
        // Get all upvote reactions for this question
        var reactions = await reactionProvider.Get()
            .WhereEquals(nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionWebPageItemID), request.QuestionWebPageItemId)
            .OrderBy(nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionID))
            .GetEnumerableTypedResultAsync();

        int totalCount = reactions.Count();
        bool currentMemberHasReacted = request.CurrentMemberId.HasValue
            && reactions.Any(r => r.QAndAQuestionReactionMemberID == request.CurrentMemberId);

        var members = (await memberProvider.Get()
            .WhereIn(nameof(MemberInfo.MemberID), reactions.Select(r => r.QAndAQuestionReactionMemberID).Distinct())
            .GetEnumerableTypedResultAsync())
            .Select(CommunityMember.FromMemberInfo)
            .ToDictionary(m => m.Id);

        var memberDisplayNames = reactions
            .Select(r => r.QAndAQuestionReactionMemberID)
            .Distinct()
            .Select(mId => members.TryGetValue(mId, out var m) ? m.DisplayName : "")
            .ToList();

        return new QuestionReactionsData(
            totalCount,
            currentMemberHasReacted,
            memberDisplayNames);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndAQuestionReactionsQuery query, QuestionReactionsData result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(QAndAQuestionReactionInfo.OBJECT_TYPE);
}
