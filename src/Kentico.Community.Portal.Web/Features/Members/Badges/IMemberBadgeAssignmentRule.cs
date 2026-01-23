using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public interface IMemberBadgeAssignmentRule
{
    public string BadgeCodeName { get; }
    public Task<IReadOnlyList<NewMemberBadgeRelationship>> Assign(
        MemberBadgeInfo memberBadge,
        MemberBadgeRelationshipDictionary memberBadgeRelationships,
        IReadOnlyList<MemberInfo> enabledMembers,
        CancellationToken cancellationToken);
}

public record struct NewMemberBadgeRelationship(int MemberID, int MemberBadgeID);
