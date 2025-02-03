using CMS.ContentEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class CommunityContentContributorMemberBadgeAssignmentRule(IContentQueryExecutor contentQueryExecutor) : IMemberBadgeAssignmentRule
{
    private readonly IContentQueryExecutor contentQueryExecutor = contentQueryExecutor;

    public string BadgeCodeName => "CommunityContentContributor";

    public async Task<IReadOnlyList<NewMemberBadgeRelationship>> Assign(
        MemberBadgeInfo memberBadge,
        MemberBadgeRelationshipDictionary memberBadgeRelationships,
        IReadOnlyList<MemberInfo> members,
        CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                LinkContent.CONTENT_TYPE_NAME,
                q => q
                    .Where(w => w.WhereGreater(nameof(LinkContent.LinkContentMemberID), 0))
                    .Columns(nameof(LinkContent.LinkContentMemberID)));

        var links = await contentQueryExecutor.GetMappedResult<LinkContent>(b, options: null, cancellationToken: cancellationToken);
        var linkMemberIDs = links
            .Select(l => l.LinkContentMemberID)
            .Distinct();

        var results = new List<NewMemberBadgeRelationship>();

        foreach (int memberID in linkMemberIDs)
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
