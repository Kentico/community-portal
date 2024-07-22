using CMS.ContentEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class DiscussionAuthorMemberBadgeAssignmentRule(IContentQueryExecutor contentQueryExecutor) : IMemberBadgeAssignmentRule
{
    private readonly IContentQueryExecutor contentQueryExecutor = contentQueryExecutor;

    public string BadgeCodeName => "DiscussionAuthor";

    public async Task<IReadOnlyList<NewMemberBadgeRelationship>> Assign(
        MemberBadgeInfo memberBadge,
        MemberBadgeRelationshipDictionary memberBadgeRelationships,
        IReadOnlyList<MemberInfo> members,
        CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                QAndAQuestionPage.CONTENT_TYPE_NAME,
                q => q
                    .Where(w => w.WhereGreater(nameof(QAndAQuestionPage.QAndAQuestionPageAuthorMemberID), 0))
                    .Columns(nameof(QAndAQuestionPage.QAndAQuestionPageAuthorMemberID)));

        var questions = await contentQueryExecutor.GetMappedResult<QAndAQuestionPage>(b, options: null, cancellationToken: cancellationToken);

        var results = new List<NewMemberBadgeRelationship>();

        foreach (var question in questions.DistinctBy(q => q.QAndAQuestionPageAuthorMemberID))
        {
            if (memberBadgeRelationships.HasEntry(question.QAndAQuestionPageAuthorMemberID, memberBadge.MemberBadgeID))
            {
                continue;
            }

            results.Add(new NewMemberBadgeRelationship(question.QAndAQuestionPageAuthorMemberID, memberBadge.MemberBadgeID));
        }

        return results;
    }
}
