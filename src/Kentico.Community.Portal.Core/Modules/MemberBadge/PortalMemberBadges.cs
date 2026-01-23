namespace Kentico.Community.Portal.Core.Modules;

public static class PortalMemberBadges
{
    public const string COMMUNITY_LEADER = "KenticoCommunityLeader";
    public const string MVP = "KenticoMVP";
    public const string KENTICO_EMPLOYEE = "KenticoEmployee";
    public const string CONTENT_MODELING_CERTIFICATION = "XperienceByKenticoContentModelingCertification";
    public const string MARKETER_CERTIFICATION = "XperienceByKenticoMarketerCertification";
    public const string DEVELOPER_CERTIFICATION = "XperienceByKenticoDeveloperCertification";
    public const string SALES_CERTIFICATION = "XperienceByKenticoSalesCertification";

    private static readonly HashSet<string> alwaysSelectedBadges =
    [
        KENTICO_EMPLOYEE,
        MVP,
        COMMUNITY_LEADER
    ];

    public static bool IsAlwaysSelected(string badgeCodeName) =>
        alwaysSelectedBadges.Contains(badgeCodeName, StringComparer.OrdinalIgnoreCase);

    public static bool IsAlwaysSelected(MemberBadgeInfo badge) =>
        IsAlwaysSelected(badge.MemberBadgeCodeName);
}
