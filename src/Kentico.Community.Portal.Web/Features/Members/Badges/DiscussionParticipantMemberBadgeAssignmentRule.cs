using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class DiscussionParticipantMemberBadgeAssignmentRule(
    IInfoProvider<QAndAAnswerDataInfo> answerDataProvider) : IMemberBadgeAssignmentRule
{
    private readonly IInfoProvider<QAndAAnswerDataInfo> answerDataProvider = answerDataProvider;

    public string BadgeCodeName => "DiscussionParticipant";

    public async Task<IReadOnlyList<NewMemberBadgeRelationship>> Assign(
        MemberBadgeInfo memberBadge,
        MemberBadgeRelationshipDictionary memberBadgeRelationships,
        IReadOnlyList<MemberInfo> members,
        CancellationToken cancellationToken)
    {
        var answers = await answerDataProvider
            .Get()
            .Columns(nameof(QAndAAnswerDataInfo.QAndAAnswerDataAuthorMemberID))
            .Distinct()
            .GetEnumerableTypedResultAsync();

        var results = new List<NewMemberBadgeRelationship>();

        foreach (var answer in answers)
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
