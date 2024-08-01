
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

public class MemberBadgeAssignmentModel
{
    public int MemberBadgeID { get; set; } = 0;
    public string MemberBadgeDescription { get; set; } = string.Empty;
    public string MemberBadgeDisplayName { get; set; } = string.Empty;
    public string? BadgeImageRelativePath { get; set; }
    public bool IsAssigned { get; set; }
    public MemberBadgeAssignmentModel() { }
    public MemberBadgeAssignmentModel(MemberBadgeInfo badge, bool isAssigned, string? badgeImageRelativePath)
    {
        MemberBadgeID = badge.MemberBadgeID;
        MemberBadgeDescription = badge.MemberBadgeShortDescription;
        MemberBadgeDisplayName = badge.MemberBadgeDisplayName;
        IsAssigned = isAssigned;
        BadgeImageRelativePath = badgeImageRelativePath?.TrimStart('~');
    }
}
