using System.Globalization;
using CMS.DataEngine.Internal;
using CMS.Membership;
using Kentico.Membership;

namespace Kentico.Community.Portal.Web.Membership;

public class CommunityMember : ApplicationUser
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName =>
        (FirstName, LastName) switch
        {
            ("", "") => "",
            (string first, "") => first,
            ("", string last) => last,
            (string first, string last) => $"{first} {last}",
            (null, null) or _ => "",
        };
    public string DisplayName =>
        string.IsNullOrWhiteSpace(FullName)
            ? UserName ?? ""
            : FullName;
    public string LinkedInIdentifier { get; set; } = "";
    public DateTime Created { get; set; }
    public string AvatarFileExtension { get; set; } = "";
    public ModerationStatuses ModerationStatus { get; set; } = ModerationStatuses.None;
    public LinkDataType EmployerLink { get; set; } = new LinkDataType();
    public string JobTitle { get; set; } = "";
    public string Country { get; set; } = "";
    public string Bio { get; set; } = "";
    public string TimeZone { get; set; } = "";

    public bool IsUnderModeration() => ModerationStatus != ModerationStatuses.None;

    public override void MapToMemberInfo(MemberInfo target)
    {
        ArgumentNullException.ThrowIfNull(target, nameof(target));

        /*
         * base.MapToMemberInfo will set target.MemberPassword everytime
         * however we do not want to set it if PasswordHash is null,
         * and this stores the original so we can revert it
         */
        string originalPasswordHash = target.MemberPassword;

        base.MapToMemberInfo(target);

        if (PasswordHash is null)
        {
            target.MemberPassword = originalPasswordHash;
        }

        _ = target.SetValue("MemberFirstName", FirstName);
        _ = target.SetValue("MemberLastName", LastName);
        _ = target.SetValue("MemberLinkedInIdentifier", LinkedInIdentifier);
        _ = target.SetValue("MemberAvatarFileExtension", AvatarFileExtension);
        _ = target.SetValue("MemberModerationStatus", ModerationStatus.ToString());
        _ = target.SetValue("MemberEmployerLink", JsonDataTypeConverter.ConvertToString(EmployerLink, "{ }", CultureInfo.InvariantCulture));
        _ = target.SetValue("MemberJobTitle", JobTitle);
        _ = target.SetValue("MemberCountry", Country);
        _ = target.SetValue("MemberBio", Bio);
        _ = target.SetValue("MemberTimeZone", TimeZone);
    }

    public override void MapFromMemberInfo(MemberInfo source)
    {
        base.MapFromMemberInfo(source);

        FirstName = source.GetValue("MemberFirstName", "");
        LastName = source.GetValue("MemberLastName", "");
        LinkedInIdentifier = source.GetValue("MemberLinkedInIdentifier", "");
        Created = source.MemberCreated;
        AvatarFileExtension = source.GetValue("MemberAvatarFileExtension", "");
        ModerationStatus = Enum.TryParse<ModerationStatuses>(source.GetValue("MemberModerationStatus", ""), out var status)
            ? status
            : ModerationStatuses.None;
        EmployerLink = JsonDataTypeConverter.ConvertToModel(source.GetValue("MemberEmployerLink", "{ }"), new LinkDataType(), CultureInfo.InvariantCulture);
        JobTitle = source.GetValue("MemberJobTitle", "");
        Country = source.GetValue("MemberCountry", "");
        Bio = source.GetValue("MemberBio", "");
        TimeZone = source.GetValue("MemberTimeZone", "");
    }

    public static CommunityMember FromMemberInfo(MemberInfo memberInfo)
    {
        var communityMember = new CommunityMember();
        communityMember.MapFromMemberInfo(memberInfo);

        return communityMember;
    }
}

public static class MemberInfoExtensions
{
    public static CommunityMember AsCommunityMember(this MemberInfo member) =>
        CommunityMember.FromMemberInfo(member);
}

public enum ModerationStatuses
{
    None,
    Spam,
    Flagged,
    Archived
}
