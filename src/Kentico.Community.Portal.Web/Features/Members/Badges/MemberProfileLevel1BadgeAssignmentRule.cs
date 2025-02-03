using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class MemberProfileLevel1BadgeAssignmentRule() : IMemberBadgeAssignmentRule
{
    public string BadgeCodeName => "MemberProfileLevel1";

    public Task<IReadOnlyList<NewMemberBadgeRelationship>> Assign(
        MemberBadgeInfo memberBadge,
        MemberBadgeRelationshipDictionary memberBadgeRelationships,
        IReadOnlyList<MemberInfo> members,
        CancellationToken cancellationToken)
    {
        var results = new List<NewMemberBadgeRelationship>();

        foreach (var member in members)
        {
            if (memberBadgeRelationships.HasEntry(member.MemberID, memberBadge.MemberBadgeID))
            {
                continue;
            }

            var communityMember = member.AsCommunityMember();

            if (string.IsNullOrWhiteSpace(communityMember.AvatarFileExtension)
                || string.IsNullOrWhiteSpace(communityMember.FullName)
                || string.IsNullOrWhiteSpace(communityMember.JobTitle)
                || string.IsNullOrWhiteSpace(communityMember.LinkedInIdentifier)
                || !communityMember.EmployerLink.HasValue)
            {
                continue;
            }

            results.Add(new NewMemberBadgeRelationship(communityMember.Id, memberBadge.MemberBadgeID));
        }

        return Task.FromResult<IReadOnlyList<NewMemberBadgeRelationship>>(results);
    }
}
