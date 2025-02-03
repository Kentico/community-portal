using CMS.ContentEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class IntegrationAuthorMemberBadgeAssignmentRule(IContentQueryExecutor contentQueryExecutor) : IMemberBadgeAssignmentRule
{
    private readonly IContentQueryExecutor contentQueryExecutor = contentQueryExecutor;

    public string BadgeCodeName => "IntegrationAuthor";

    public async Task<IReadOnlyList<NewMemberBadgeRelationship>> Assign(
        MemberBadgeInfo memberBadge,
        MemberBadgeRelationshipDictionary memberBadgeRelationships,
        IReadOnlyList<MemberInfo> members,
        CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                IntegrationContent.CONTENT_TYPE_NAME,
                q => q
                    .Where(w => w.WhereGreater(nameof(IntegrationContent.IntegrationContentAuthorMemberID), 0))
                    .Columns(nameof(IntegrationContent.IntegrationContentAuthorMemberID)));

        var integrations = await contentQueryExecutor.GetMappedResult<IntegrationContent>(b, options: null, cancellationToken: cancellationToken);
        var integrationMemberIDs = integrations
            .Select(i => i.IntegrationContentAuthorMemberID)
            .Distinct();

        var results = new List<NewMemberBadgeRelationship>();

        foreach (int memberID in integrationMemberIDs)
        {
            if (memberBadgeRelationships.HasEntry(memberID, memberBadge.MemberBadgeID))
            {
                continue;
            }

            results.Add(new NewMemberBadgeRelationship(memberID, memberBadge.MemberBadgeID));
        }

        return results;
    }
}
