using System.Collections.Immutable;
using CMS.Membership;

namespace Kentico.Community.Portal.Core.Modules;

public static class SystemMemberRoles
{
    public static KenticoMVPRole KenticoMVP { get; } = new();
    public static KenticoCommunityLeaderRole KenticoCommunityLeader { get; } = new();
    public static KenticoEmployeeRole KenticoEmployee { get; } = new();

    public static readonly ImmutableList<ISystemMemberRole> ProtectedMemberRoles =
    [
        KenticoMVP,
        KenticoCommunityLeader,
        KenticoEmployee
    ];

    public static bool Includes(MemberRoleInfo role) =>
        ProtectedMemberRoles.Select(r => r.MemberRoleGUID).Contains(role.MemberRoleGUID);

    public record KenticoMVPRole : ISystemMemberRole
    {
        public static Guid GUID { get; } = new Guid("4fd01b44-1d57-4cb6-a377-7c822ca555a5");
        public const string CodeName = "KenticoMVP";

        public Guid MemberRoleGUID => GUID;

        public string MemberRoleName => CodeName;
    }

    public record KenticoCommunityLeaderRole : ISystemMemberRole
    {
        public static Guid GUID { get; } = new Guid("958bceb5-d536-4e87-b3eb-c2a302a9089c");
        public const string CodeName = "KenticoCommunityLeader";

        public Guid MemberRoleGUID => GUID;

        public string MemberRoleName => CodeName;
    }

    public record KenticoEmployeeRole : ISystemMemberRole
    {
        public static Guid GUID { get; } = new Guid("fbc67f69-c436-49f8-8597-0aa8524c511d");
        public const string CodeName = "KenticoEmployee";

        public Guid MemberRoleGUID => GUID;

        public string MemberRoleName => CodeName;
    }
}

public interface ISystemMemberRole
{
    public Guid MemberRoleGUID { get; }

    public string MemberRoleName { get; }
}
