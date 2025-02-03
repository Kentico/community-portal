using CMS.ContentEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class BlogPostAuthorMemberBadgeAssignmentRule(IContentQueryExecutor contentQueryExecutor) : IMemberBadgeAssignmentRule
{
    private readonly IContentQueryExecutor contentQueryExecutor = contentQueryExecutor;

    public string BadgeCodeName => "CommunityBlogPostAuthor";

    public async Task<IReadOnlyList<NewMemberBadgeRelationship>> Assign(
        MemberBadgeInfo memberBadge,
        MemberBadgeRelationshipDictionary memberBadgeRelationships,
        IReadOnlyList<MemberInfo> members,
        CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                AuthorContent.CONTENT_TYPE_NAME,
                q => q
                    .Where(w => w.WhereGreater(nameof(AuthorContent.AuthorContentMemberID), 0))
                    .Columns(nameof(AuthorContent.AuthorContentMemberID)));

        var authors = await contentQueryExecutor.GetMappedResult<AuthorContent>(b, options: null, cancellationToken: cancellationToken);
        var authorMemberIDs = authors
            .Select(a => a.AuthorContentMemberID)
            .Distinct();

        var results = new List<NewMemberBadgeRelationship>();

        foreach (int memberID in authorMemberIDs)
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
