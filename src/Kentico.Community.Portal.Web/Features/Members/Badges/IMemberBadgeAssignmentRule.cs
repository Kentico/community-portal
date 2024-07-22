using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.Members.Badges;

public interface IMemberBadgeAssignmentRule
{
    string BadgeCodeName { get; }
    Task<IReadOnlyList<NewMemberBadgeRelationship>> Assign(
        MemberBadgeInfo memberBadge,
        MemberBadgeRelationshipDictionary memberBadgeRelationships,
        IReadOnlyList<MemberInfo> enabledMembers,
        CancellationToken cancellationToken);
}

public record struct NewMemberBadgeRelationship(int MemberID, int MemberBadgeID);
