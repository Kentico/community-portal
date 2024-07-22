using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public class EarlyAdopterMemberBadgeAssignmentRule() : IMemberBadgeAssignmentRule
{
    public string BadgeCodeName => "EarlyAdopter";

    public static DateTime EarlyAdopterCutoffDate { get; } = new DateTime(2024, 6, 30);

    public Task<IReadOnlyList<NewMemberBadgeRelationship>> Assign(
        MemberBadgeInfo memberBadge,
        MemberBadgeRelationshipDictionary memberBadgeRelationships,
        IReadOnlyList<MemberInfo> enabledMembers,
        CancellationToken cancellationToken)
    {
        var results = new List<NewMemberBadgeRelationship>();

        foreach (var member in enabledMembers)
        {
            if (member.MemberCreated > EarlyAdopterCutoffDate || memberBadgeRelationships.HasEntry(member.MemberID, memberBadge.MemberBadgeID))
            {
                continue;
            }

            results.Add(new NewMemberBadgeRelationship(member.MemberID, memberBadge.MemberBadgeID));
        }

        return Task.FromResult<IReadOnlyList<NewMemberBadgeRelationship>>(results);
    }
}
