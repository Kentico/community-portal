using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class MemberAnniversary3YearMemberBadgeAssignmentRule(TimeProvider time) : IMemberBadgeAssignmentRule
{
    private readonly TimeProvider time = time;

    public string BadgeCodeName => "MemberAnniversary3Year";

    public Task<IReadOnlyList<NewMemberBadgeRelationship>> Assign(
        MemberBadgeInfo memberBadge,
        MemberBadgeRelationshipDictionary memberBadgeRelationships,
        IReadOnlyList<MemberInfo> members,
        CancellationToken cancellationToken)
    {
        var now = time.GetLocalNow();
        var results = new List<NewMemberBadgeRelationship>();

        foreach (var member in members)
        {
            if (memberBadgeRelationships.HasEntry(member.MemberID, memberBadge.MemberBadgeID))
            {
                continue;
            }

            if (now < member.MemberCreated.AddYears(3))
            {
                continue;
            }

            results.Add(new NewMemberBadgeRelationship(member.MemberID, memberBadge.MemberBadgeID));
        }

        return Task.FromResult<IReadOnlyList<NewMemberBadgeRelationship>>(results);
    }
}
