using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;
using CMS.Membership;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerDatasByQuestionQuery(int QuestionWebPageItemID, int? CurrentMemberId = null) : IQuery<QAndAAnswerDatasByQuestionQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => $"{QuestionWebPageItemID}_{CurrentMemberId}";
}

public record AnswerWithReactionsData(
    QAndAAnswerDataInfo Answer,
    AnswerReactionsData Reactions);

public record QAndAAnswerDatasByQuestionQueryResponse(IReadOnlyList<AnswerWithReactionsData> Items);
public class QAndAAnswerDatasByQuestionQueryHandler(
    DataItemQueryTools tools,
    IInfoProvider<QAndAAnswerDataInfo> answerProvider,
    IInfoProvider<QAndAAnswerReactionInfo> reactionProvider,
    IInfoProvider<MemberInfo> memberProvider)
    : DataItemQueryHandler<QAndAAnswerDatasByQuestionQuery, QAndAAnswerDatasByQuestionQueryResponse>(tools)
{
    private readonly IInfoProvider<QAndAAnswerDataInfo> answerProvider = answerProvider;
    private readonly IInfoProvider<QAndAAnswerReactionInfo> reactionProvider = reactionProvider;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;

    public override async Task<QAndAAnswerDatasByQuestionQueryResponse> Handle(QAndAAnswerDatasByQuestionQuery request, CancellationToken cancellationToken)
    {
        // Get all answers for the question
        var answers = await answerProvider.Get()
            .WhereEquals(nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID), request.QuestionWebPageItemID)
            .GetEnumerableTypedResultAsync();

        var allReactions = await reactionProvider.Get()
            .WhereIn(nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionAnswerID), answers.Select(a => a.QAndAAnswerDataID))
            .OrderByAscending(nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionID))
            .GetEnumerableTypedResultAsync();

        // Get all member info for reacting members
        var members = (await memberProvider.Get()
            .WhereIn(nameof(MemberInfo.MemberID), allReactions.Select(r => r.QAndAAnswerReactionMemberID).Distinct())
            .GetEnumerableTypedResultAsync())
            .Select(CommunityMember.FromMemberInfo)
            .ToDictionary(m => m.Id);

        // Build response with reaction data for each answer
        var result = new List<AnswerWithReactionsData>();
        foreach (var answer in answers)
        {
            var answerReactions = allReactions.Where(r => r.QAndAAnswerReactionAnswerID == answer.QAndAAnswerDataID).ToList();
            int totalCount = answerReactions.Count;
            bool currentMemberHasReacted = request.CurrentMemberId.HasValue &&
                answerReactions.Any(r => r.QAndAAnswerReactionMemberID == request.CurrentMemberId);

            var memberDisplayNames = answerReactions
                .Select(r => r.QAndAAnswerReactionMemberID)
                .Distinct()
                .Select(mId => members.TryGetValue(mId, out var m) ? m.DisplayName : "")
                .ToList();

            result.Add(new AnswerWithReactionsData(
                answer,
                new(totalCount, currentMemberHasReacted, memberDisplayNames)));
        }

        return new(result);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        QAndAAnswerDatasByQuestionQuery query,
        QAndAAnswerDatasByQuestionQueryResponse result,
        ICacheDependencyKeysBuilder builder) =>
        builder
            .AllObjects(QAndAAnswerDataInfo.OBJECT_TYPE)
            .AllObjects(QAndAAnswerReactionInfo.OBJECT_TYPE)
            .AllObjects(MemberInfo.OBJECT_TYPE);
}
