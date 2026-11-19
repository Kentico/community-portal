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
    /// <summary>The <see cref="TermsOptions.CurrentVersion" /> the member accepted. Empty for members who registered before Terms acceptance.</summary>
    public string TermsAcceptedVersion { get; set; } = "";
    public DateTime? TermsAcceptedDate { get; set; }
    /// <summary>Set when the member asks for the newsletter at registration. Cleared once subscribed, on email confirmation.</summary>
    public DateTime? NewsletterOptInRequestedDate { get; set; }
    /// <summary>A second email (e.g. business or agency) set by the member. Not trusted until <see cref="AlternateEmailVerifiedDate"/> is set.</summary>
    public string AlternateEmail { get; set; } = "";
    /// <summary>When the member proved they own <see cref="AlternateEmail"/>. Null = unverified.</summary>
    public DateTime? AlternateEmailVerifiedDate { get; set; }

    /// <summary>
    /// When the member proved they own their account <see cref="Kentico.Membership.ApplicationUser.Email"/> (registration
    /// confirmation or a confirmed email change), or an administrator enabled them. Null = never confirmed.
    /// Unlike <see cref="Kentico.Membership.ApplicationUser.Enabled"/> (Xperience's EmailConfirmed), disabling the member doesn't clear it.
    /// </summary>
    public DateTime? EmailConfirmedDate { get; set; }

    /// <summary>
    /// Why the member can or can't sign in. See docs/adr/0005-separate-email-confirmation-from-enabled.md.
    /// </summary>
    public MemberSignInState SignInState =>
        (IsUnderModeration, Enabled, EmailConfirmedDate) switch
        {
            (true, _, _) => MemberSignInState.UnderModeration,
            (false, true, _) => MemberSignInState.Active,
            (false, false, null) => MemberSignInState.EmailNotConfirmed,
            (false, false, not null) => MemberSignInState.Disabled,
        };

    /// <summary>
    /// Only a member who never confirmed their email can request a registration confirmation email or confirm it.
    /// A disabled member (confirmed before) can't re-enable themselves this way.
    /// </summary>
    public bool CanConfirmEmail => SignInState == MemberSignInState.EmailNotConfirmed;

    public bool IsAlternateEmailVerified =>
        !string.IsNullOrWhiteSpace(AlternateEmail) && AlternateEmailVerifiedDate is not null;

    /// <summary>The <see cref="AlternateEmail"/> only when verified, otherwise null. Use this, never <see cref="AlternateEmail"/>, to match a member.</summary>
    public string? VerifiedAlternateEmail =>
        IsAlternateEmailVerified ? AlternateEmail : null;

    public bool IsUnderModeration =>
        ModerationStatus != ModerationStatuses.None;

    /*
     * Note that EmailConfirmed and Enabled are the same value/state with Xperience's membership
     * https://docs.kentico.com/documentation/developers-and-admins/development/registration-and-authentication#applicationuser.enabled
     *
     * Members can change their email (MemberEmailVerificationService), but the Email only changes
     * after the new address is verified (UserManager.ChangeEmailAsync, which also rotates the security stamp),
     * so a confirmed member's Email is always an address they proved they own.
     */
    public bool IsInternalEmployee =>
        Email is not null &&
        EmailConfirmed &&
        Email.EndsWith("@kentico.com", StringComparison.OrdinalIgnoreCase);

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
        target.MemberTermsAcceptedVersion = TermsAcceptedVersion;
        target.MemberTermsAcceptedDate = TermsAcceptedDate;
        target.MemberNewsletterOptInRequestedDate = NewsletterOptInRequestedDate;
        target.MemberAlternateEmail = AlternateEmail;
        target.MemberAlternateEmailVerifiedDate = AlternateEmailVerifiedDate;
        target.MemberEmailConfirmedDate = EmailConfirmedDate;
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
        TermsAcceptedVersion = source.MemberTermsAcceptedVersion;
        TermsAcceptedDate = source.MemberTermsAcceptedDate;
        NewsletterOptInRequestedDate = source.MemberNewsletterOptInRequestedDate;
        AlternateEmail = source.MemberAlternateEmail;
        AlternateEmailVerifiedDate = source.MemberAlternateEmailVerifiedDate;
        EmailConfirmedDate = source.MemberEmailConfirmedDate;
        EmailConfirmed = source.MemberEnabled;
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

public enum MemberSignInState
{
    /// <summary>Enabled and not moderated.</summary>
    Active,
    /// <summary>Moderation status other than None. Wins over every other state.</summary>
    UnderModeration,
    /// <summary>Not enabled, <see cref="CommunityMember.EmailConfirmedDate"/> null: registered, email not confirmed yet.</summary>
    EmailNotConfirmed,
    /// <summary>Not enabled, but the email was confirmed before: an administrator disabled the member.</summary>
    Disabled,
}

[ValueObject<int>]
[Instance("Anonymous", 0)]
public readonly partial struct CommunityMemberID;

public static class MemberInfoExtensions
{
    public const string FIELD_ALTERNATE_EMAIL = "MemberAlternateEmail";
    public const string FIELD_ALTERNATE_EMAIL_VERIFIED_DATE = "MemberAlternateEmailVerifiedDate";
    public const string FIELD_EMAIL_CONFIRMED_DATE = "MemberEmailConfirmedDate";

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

        public string MemberTermsAcceptedVersion
        {
            get => member.GetValue("MemberTermsAcceptedVersion", "") ?? "";
            set => member.SetValue("MemberTermsAcceptedVersion", value);
        }

        public DateTime? MemberTermsAcceptedDate
        {
            get => member.GetValue("MemberTermsAcceptedDate") as DateTime?;
            set => member.SetValue("MemberTermsAcceptedDate", value);
        }

        public DateTime? MemberNewsletterOptInRequestedDate
        {
            get => member.GetValue("MemberNewsletterOptInRequestedDate") as DateTime?;
            set => member.SetValue("MemberNewsletterOptInRequestedDate", value);
        }

        public string MemberAlternateEmail
        {
            get => member.GetValue(FIELD_ALTERNATE_EMAIL, "") ?? "";
            set => member.SetValue(FIELD_ALTERNATE_EMAIL, value);
        }

        public DateTime? MemberAlternateEmailVerifiedDate
        {
            get => member.GetValue(FIELD_ALTERNATE_EMAIL_VERIFIED_DATE) as DateTime?;
            set => member.SetValue(FIELD_ALTERNATE_EMAIL_VERIFIED_DATE, value);
        }

        public DateTime? MemberEmailConfirmedDate
        {
            get => member.GetValue(FIELD_EMAIL_CONFIRMED_DATE) as DateTime?;
            set => member.SetValue(FIELD_EMAIL_CONFIRMED_DATE, value);
        }
    }
}


