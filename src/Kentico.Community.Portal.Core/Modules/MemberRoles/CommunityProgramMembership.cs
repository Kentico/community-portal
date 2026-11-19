using System.ComponentModel;

namespace Kentico.Community.Portal.Core.Modules;

/// <summary>
/// A member's Community Program tier, computed from <see cref="SystemMemberRoles.KenticoMVP"/> /
/// <see cref="SystemMemberRoles.KenticoCommunityLeader"/> Member Role membership. Not persisted -
/// Member Roles are the source of truth. See docs/adr/0002-program-status-as-member-roles.md.
/// </summary>
public enum CommunityProgramMembership
{
    [Description("None")]
    None,
    [Description("MVP")]
    MVP,
    [Description("Community Leader")]
    CommunityLeader
}
