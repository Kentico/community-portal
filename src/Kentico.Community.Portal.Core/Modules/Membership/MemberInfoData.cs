using System.ComponentModel;
using CMS.Membership;

namespace Kentico.Community.Portal.Core.Modules.Membership;

public static class CommunityMemberInfoExtensions
{
    public const string FIELD_MODERATION_STATUS = "MemberModerationStatus";

    extension(MemberInfo member)
    {
        public string MemberModerationStatus
        {
            get => member.GetStringValue(FIELD_MODERATION_STATUS, "");
            set => member.SetValue(FIELD_MODERATION_STATUS, value);
        }
    }
}

public enum ModerationStatuses
{
    None,
    Spam,
    Flagged,
    Archived
}

public enum MemberStates
{
    Enabled,
    Disabled
}
