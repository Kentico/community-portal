using System.Globalization;
using System.Security.Claims;
using CMS.DataEngine.Internal;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Membership;
using Vogen;

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

    public ProgramStatuses ProgramStatus { get; set; } = ProgramStatuses.None;

    public bool IsUnderModeration =>
        ModerationStatus != ModerationStatuses.None;

    /*
     * Note that EmailConfirmed and Enabled are the same value/state with Xperience's membership
     * https://docs.kentico.com/documentation/developers-and-admins/development/registration-and-authentication#applicationuser.enabled
     *
     * Once confirmed / enabled, members cannot change their email address themselves.
     * If this functionality is added in the future, 
     * WE MUST CHANGE THIS CHECK OR ENABLE TRUE EMAIL CONFIRMATION by resetting the security stamp after an email change
     * or having a unique state in the database for unconfirmed email addresses.
     */
    public bool IsInternalEmployee =>
        Email is not null &&
        EmailConfirmed &&
        Email.EndsWith("@kentico.com", StringComparison.OrdinalIgnoreCase);

    public bool IsCommunityProgramMember =>
        ProgramStatus != ProgramStatuses.None;

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

        target.MemberFirstName = FirstName;
        target.MemberLastName = LastName;
        target.MemberLinkedInIdentifier = LinkedInIdentifier;
        target.MemberAvatarFileExtension = AvatarFileExtension;
        target.MemberModerationStatus = ModerationStatus.ToString();
        target.MemberEmployerLink = (string)JsonDataTypeConverter.ConvertToString(EmployerLink, "{ }", CultureInfo.InvariantCulture);
        target.MemberJobTitle = JobTitle;
        target.MemberCountry = Country;
        target.MemberBio = Bio;
        target.MemberTimeZone = TimeZone;
        target.MemberProgramStatus = ProgramStatus.ToString();
    }

    public override void MapFromMemberInfo(MemberInfo source)
    {
        base.MapFromMemberInfo(source);

        FirstName = source.MemberFirstName;
        LastName = source.MemberLastName;
        LinkedInIdentifier = source.MemberLinkedInIdentifier;
        Created = source.MemberCreated;
        AvatarFileExtension = source.MemberAvatarFileExtension;
        ModerationStatus = Enum.TryParse<ModerationStatuses>(source.MemberModerationStatus, out var status)
            ? status
            : ModerationStatuses.None;
        EmployerLink = JsonDataTypeConverter.ConvertToModel(source.MemberEmployerLink, new LinkDataType(), CultureInfo.InvariantCulture);
        JobTitle = source.MemberJobTitle;
        Country = source.MemberCountry;
        Bio = source.MemberBio;
        TimeZone = source.MemberTimeZone;
        EmailConfirmed = source.MemberEnabled;

        ProgramStatus = Enum.TryParse<ProgramStatuses>(source.MemberProgramStatus, out var programStatus)
            ? programStatus
            : ProgramStatuses.None;
    }

    public static CommunityMember FromMemberInfo(MemberInfo memberInfo)
    {
        var communityMember = new CommunityMember();
        communityMember.MapFromMemberInfo(memberInfo);

        return communityMember;
    }

    public static CommunityMemberID GetMemberIDFromClaim(HttpContext? ctx)
    {
        var identity = ctx?.User.Identity;
        if (ctx is null
            || ctx.User.Identity is not ClaimsIdentity claimsIdentity
            || !claimsIdentity.IsAuthenticated
            || claimsIdentity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) is not Claim idClaim
            || !int.TryParse(idClaim.Value, out int id))
        {
            return CommunityMemberID.Anonymous;
        }

        return CommunityMemberID.From(id);
    }
}

[ValueObject<int>]
[Instance("Anonymous", 0)]
public readonly partial struct CommunityMemberID;

public static class MemberInfoExtensions
{
    public const string FIELD_PROGRAM_STATUS = "MemberProgramStatus";

    public static CommunityMember AsCommunityMember(this MemberInfo member) =>
        CommunityMember.FromMemberInfo(member);

    extension(MemberInfo member)
    {
        public string MemberFirstName
        {
            get => member.GetValue("MemberFirstName", "") ?? "";
            set => member.SetValue("MemberFirstName", value);
        }

        public string MemberLastName
        {
            get => member.GetValue("MemberLastName", "") ?? "";
            set => member.SetValue("MemberLastName", value);
        }

        public string MemberLinkedInIdentifier
        {
            get => member.GetValue("MemberLinkedInIdentifier", "") ?? "";
            set => member.SetValue("MemberLinkedInIdentifier", value);
        }

        public string MemberAvatarFileExtension
        {
            get => member.GetValue("MemberAvatarFileExtension", "") ?? "";
            set => member.SetValue("MemberAvatarFileExtension", value);
        }

        public string MemberModerationStatus
        {
            get => member.GetValue("MemberModerationStatus", "") ?? "";
            set => member.SetValue("MemberModerationStatus", value);
        }

        public string MemberEmployerLink
        {
            get => member.GetValue("MemberEmployerLink", "{ }") ?? "{ }";
            set => member.SetValue("MemberEmployerLink", value);
        }

        public string MemberJobTitle
        {
            get => member.GetValue("MemberJobTitle", "") ?? "";
            set => member.SetValue("MemberJobTitle", value);
        }

        public string MemberCountry
        {
            get => member.GetValue("MemberCountry", "") ?? "";
            set => member.SetValue("MemberCountry", value);
        }

        public string MemberBio
        {
            get => member.GetValue("MemberBio", "") ?? "";
            set => member.SetValue("MemberBio", value);
        }

        public string MemberTimeZone
        {
            get => member.GetValue("MemberTimeZone", "") ?? "";
            set => member.SetValue("MemberTimeZone", value);
        }
    }
}


