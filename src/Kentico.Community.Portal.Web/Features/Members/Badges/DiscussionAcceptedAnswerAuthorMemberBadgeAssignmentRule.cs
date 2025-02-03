using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class DiscussionAcceptedAnswerAuthorMemberBadgeAssignmentRule(IInfoProvider<QAndAAnswerDataInfo> answerDataProvider) : IMemberBadgeAssignmentRule
{
    private readonly IInfoProvider<QAndAAnswerDataInfo> answerDataProvider = answerDataProvider;

    public string BadgeCodeName => "DiscussionAcceptedAnswerAuthor";

    public async Task<IReadOnlyList<NewMemberBadgeRelationship>> Assign(
        MemberBadgeInfo memberBadge,
        MemberBadgeRelationshipDictionary memberBadgeRelationships,
        IReadOnlyList<MemberInfo> members,
        CancellationToken cancellationToken)
    {
        var acceptedAnswers = await answerDataProvider
            .Get()
            .Source(s => s.Join("KenticoCommunity_QAndAQuestionPage", nameof(QAndAAnswerDataInfo.QAndAAnswerDataGUID), nameof(QAndAQuestionPage.QAndAQuestionPageAcceptedAnswerDataGUID)))
            .Columns(nameof(QAndAAnswerDataInfo.QAndAAnswerDataAuthorMemberID))
            .WhereGreaterThan(nameof(QAndAAnswerDataInfo.QAndAAnswerDataAuthorMemberID), 0)
            .Distinct()
            .GetEnumerableTypedResultAsync();

        var results = new List<NewMemberBadgeRelationship>();

        foreach (var answer in acceptedAnswers)
        {
            if (memberBadgeRelationships.HasEntry(answer.QAndAAnswerDataAuthorMemberID, memberBadge.MemberBadgeID))
            {
                continue;
            }

            results.Add(new NewMemberBadgeRelationship(answer.QAndAAnswerDataAuthorMemberID, memberBadge.MemberBadgeID));
        }

        return results;
    }
}
