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

        var results = new List<NewMemberBadgeRelationship>();

        foreach (var author in authors)
        {
            if (memberBadgeRelationships.HasEntry(author.AuthorContentMemberID, memberBadge.MemberBadgeID))
            {
                continue;
            }

            results.Add(new NewMemberBadgeRelationship(author.AuthorContentMemberID, memberBadge.MemberBadgeID));
        }

        return results;
    }
}
