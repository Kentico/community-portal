using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class KenticoEmployeeMemberBadgeAssignmentRule : IMemberBadgeAssignmentRule
{
    public string BadgeCodeName => "KenticoEmployee";

    public Task<IReadOnlyList<NewMemberBadgeRelationship>> Assign(
        MemberBadgeInfo memberBadge,
        MemberBadgeRelationshipDictionary memberBadgeRelationships,
        IReadOnlyList<MemberInfo> enabledMembers,
        CancellationToken cancellationToken)
    {
        var results = new List<NewMemberBadgeRelationship>();

        foreach (var member in enabledMembers)
        {
            var communityMember = CommunityMember.FromMemberInfo(member);
            if (!communityMember.IsInternalEmployee
                || memberBadgeRelationships.HasEntry(member.MemberID, memberBadge.MemberBadgeID))
            {
                continue;
            }

            results.Add(new NewMemberBadgeRelationship(member.MemberID, memberBadge.MemberBadgeID));
        }

        return Task.FromResult<IReadOnlyList<NewMemberBadgeRelationship>>(results);
    }
}
