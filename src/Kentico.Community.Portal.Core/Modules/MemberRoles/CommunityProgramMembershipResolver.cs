using CMS.DataEngine;
using CMS.Membership;

namespace Kentico.Community.Portal.Core.Modules;

/// <summary>
/// Computes each member's <see cref="CommunityProgramMembership"/> from their
/// <see cref="SystemMemberRoles.KenticoMVP"/> / <see cref="SystemMemberRoles.KenticoCommunityLeader"/>
/// Member Role membership.
/// </summary>
public static class CommunityProgramMembershipResolver
{
    /// <param name="memberIds">Restricts the lookup to these members, or null to resolve every program member.</param>
    public static async Task<IReadOnlyDictionary<int, CommunityProgramMembership>> GetMembershipByMemberId(
        IInfoProvider<MemberRoleInfo> memberRoleProvider,
        IInfoProvider<MemberRoleMemberInfo> memberRoleMemberProvider,
        IReadOnlyCollection<int>? memberIds,
        CancellationToken cancellationToken = default)
    {
        var programRoles = await memberRoleProvider.Get()
            .WhereIn(nameof(MemberRoleInfo.MemberRoleGUID), new[]
            {
                SystemMemberRoles.KenticoMVP.MemberRoleGUID,
                SystemMemberRoles.KenticoCommunityLeader.MemberRoleGUID
            })
            .Columns(nameof(MemberRoleInfo.MemberRoleID), nameof(MemberRoleInfo.MemberRoleGUID))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        int mvpRoleId = programRoles.FirstOrDefault(r => r.MemberRoleGUID == SystemMemberRoles.KenticoMVP.MemberRoleGUID)?.MemberRoleID ?? 0;
        int communityLeaderRoleId = programRoles.FirstOrDefault(r => r.MemberRoleGUID == SystemMemberRoles.KenticoCommunityLeader.MemberRoleGUID)?.MemberRoleID ?? 0;

        if (mvpRoleId == 0 && communityLeaderRoleId == 0)
        {
            return new Dictionary<int, CommunityProgramMembership>();
        }

        var query = memberRoleMemberProvider.Get()
            .WhereIn(nameof(MemberRoleMemberInfo.MemberRoleMemberMemberRoleID), new[] { mvpRoleId, communityLeaderRoleId });

        if (memberIds is not null)
        {
            query = query.WhereIn(nameof(MemberRoleMemberInfo.MemberRoleMemberMemberID), memberIds.ToList());
        }

        var roleMemberships = await query
            .Columns(nameof(MemberRoleMemberInfo.MemberRoleMemberMemberID), nameof(MemberRoleMemberInfo.MemberRoleMemberMemberRoleID))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return DetermineProgramMembership(
            roleMemberships.Select(rm => (rm.MemberRoleMemberMemberID, rm.MemberRoleMemberMemberRoleID)),
            mvpRoleId,
            communityLeaderRoleId);
    }

    /// <summary>
    /// Reduces each member's role-membership rows to a single <see cref="CommunityProgramMembership"/>.
    /// MVP wins if a member somehow holds both program roles at once.
    /// </summary>
    public static Dictionary<int, CommunityProgramMembership> DetermineProgramMembership(
        IEnumerable<(int MemberId, int RoleId)> memberships,
        int mvpRoleId,
        int communityLeaderRoleId)
    {
        var membershipByMemberId = new Dictionary<int, CommunityProgramMembership>();

        foreach (var (memberId, roleId) in memberships)
        {
            var membership = roleId == mvpRoleId
                ? CommunityProgramMembership.MVP
                : roleId == communityLeaderRoleId
                    ? CommunityProgramMembership.CommunityLeader
                    : CommunityProgramMembership.None;

            if (membership == CommunityProgramMembership.None)
            {
                continue;
            }

            if (!membershipByMemberId.TryGetValue(memberId, out var existing) || existing != CommunityProgramMembership.MVP)
            {
                membershipByMemberId[memberId] = membership;
            }
        }

        return membershipByMemberId;
    }
}
