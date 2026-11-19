using CMS.Membership;
using Kentico.Community.Portal.Core.Modules.Membership;

namespace Kentico.Community.Portal.Web.Features.Members.Events;

/// <summary>
/// Rotates a member's security stamp when their access-relevant state (enabled / moderation status)
/// changes, so SecurityStampValidator (ValidationInterval = Zero) signs out existing sessions on the next request.
/// </summary>
public static class MemberSecurityStampRotation
{
    private const string FIELD_SECURITY_STAMP = "MemberSecurityStamp";

    private static readonly string[] accessColumns =
    [
        nameof(MemberInfo.MemberEnabled),
        CommunityMemberInfoExtensions.FIELD_MODERATION_STATUS
    ];

    public static bool ShouldRotate(IEnumerable<string> changedColumns) =>
        changedColumns.Any(c => accessColumns.Contains(c, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Call from MemberInfo Update.Before - sets the stamp on the object being saved, so no second save is needed.
    /// </summary>
    public static void RotateIfAccessChanged(MemberInfo member)
    {
        if (ShouldRotate(member.ChangedColumns()))
        {
            member.SetValue(FIELD_SECURITY_STAMP, Guid.NewGuid().ToString("N").ToUpperInvariant());
        }
    }
}
