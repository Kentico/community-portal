using System.ComponentModel;
using CMS.Membership;

namespace Kentico.Community.Portal.Core.Modules.Membership;

public static class CommunityMemberInfoExtensions
{
    public const string FIELD_MODERATION_STATUS = "MemberModerationStatus";
    public const string FIELD_PROGRAM_STATUS = "MemberProgramStatus";

    extension(MemberInfo member)
    {
        public string MemberModerationStatus
        {
            get => member.GetStringValue(FIELD_MODERATION_STATUS, "");
            set => member.SetValue(FIELD_MODERATION_STATUS, value);
        }

        public string MemberProgramStatus
        {
            get => member.GetStringValue(FIELD_PROGRAM_STATUS, "");
            set => member.SetValue(FIELD_PROGRAM_STATUS, value);
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

public enum ProgramStatuses
{
    [Description("None")]
    None,
    [Description("MVP")]
    MVP,
    [Description("Community Leader")]
    CommunityLeader
}

public enum MemberStates
{
    Enabled,
    Disabled
}
