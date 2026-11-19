using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Membership;
using Microsoft.AspNetCore.Identity;

namespace Kentico.Community.Portal.Web.Features.Members.EmailVerification;

public enum EmailVerificationRequestResult
{
    /// <summary>The verification email was sent.</summary>
    Sent,
    /// <summary>The email is the member's current account email.</summary>
    SameAsAccountEmail,
    /// <summary>Another member uses the email as their account email or verified alternate email.</summary>
    InUse,
    /// <summary>The alternate email is already verified.</summary>
    AlreadyVerified,
    /// <summary>The member has no alternate email to verify.</summary>
    NotSet,
}

public enum EmailVerificationConfirmResult
{
    Verified,
    AlreadyVerified,
    /// <summary>Another member took the email after the link was sent.</summary>
    InUse,
    /// <summary>Unknown or blocked member, an email that no longer matches, or an expired / tampered token.</summary>
    Invalid,
}

/// <param name="IsAccountEmailMatch">The email matched the member's account email (not their verified alternate email).</param>
public record IntegrationMatchCandidate(int MemberID, bool IsAccountEmailMatch);

/// <summary>
/// Self-service, verified changes to a member's account email and alternate email.
/// Nothing is trusted until the member clicks the link emailed to the address.
/// </summary>
/// <remarks>
/// Account email: <see cref="UserManager{TUser}.GenerateChangeEmailTokenAsync"/> /
/// <see cref="UserManager{TUser}.ChangeEmailAsync"/>. The token is bound to the new email and the
/// security stamp, and the email only changes on confirmation. The username never changes.
/// Alternate email: the same data protection token provider, with a purpose bound to the alternate email.
/// Tokens expire after <see cref="DataProtectionTokenProviderOptions.TokenLifespan"/> (default 1 day).
/// </remarks>
public class MemberEmailVerificationService(
    UserManager<CommunityMember> userManager,
    IInfoProvider<MemberInfo> memberProvider,
    IMemberEmailService emailService,
    MemberContactManager contactManager,
    LinkGenerator linkGenerator,
    IHttpContextAccessor httpContextAccessor,
    ILogger<MemberEmailVerificationService> logger) : IMemberEmailInUseChecker
{
    public const string SUPPORT_PATH = "/support/feedback";

    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly IMemberEmailService emailService = emailService;
    private readonly MemberContactManager contactManager = contactManager;
    private readonly LinkGenerator linkGenerator = linkGenerator;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly ILogger<MemberEmailVerificationService> logger = logger;

    public static string AlternateEmailTokenPurpose(string email) =>
        $"AlternateEmail:{NormalizeEmail(email)}";

    public static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    public static bool EmailsMatch(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a)
        && !string.IsNullOrWhiteSpace(b)
        && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Emails a link to <paramref name="newEmail"/>. The account email changes in <see cref="ConfirmEmailChange"/>.
    /// The caller re-authenticates the member (current password) first.
    /// </summary>
    public async Task<EmailVerificationRequestResult> RequestEmailChange(CommunityMember member, string newEmail)
    {
        newEmail = newEmail.Trim();

        if (EmailsMatch(member.Email, newEmail))
        {
            return EmailVerificationRequestResult.SameAsAccountEmail;
        }

        if (await IsEmailInUse(newEmail, member.Id))
        {
            return EmailVerificationRequestResult.InUse;
        }

        string token = await userManager.GenerateChangeEmailTokenAsync(member, newEmail);
        string url = GetVerificationURL(nameof(MemberEmailController.ConfirmEmailChange), member.Id, newEmail, token);

        await emailService.SendEmail(member, MemberEmailConfiguration.EmailChangeVerification(url), newEmail);

        return EmailVerificationRequestResult.Sent;
    }

    /// <summary>
    /// Changes the account email, then moves the member's contact to it, re-evaluates the
    /// KenticoEmployee role, and notifies the previous email. Works whether or not the member is signed in.
    /// </summary>
    public async Task<(EmailVerificationConfirmResult Result, CommunityMember? Member)> ConfirmEmailChange(int memberID, string newEmail, string token)
    {
        var member = await FindActiveMember(memberID);
        if (member is null || string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(token))
        {
            return (EmailVerificationConfirmResult.Invalid, null);
        }

        newEmail = newEmail.Trim();

        if (EmailsMatch(member.Email, newEmail))
        {
            return (EmailVerificationConfirmResult.AlreadyVerified, member);
        }

        if (await IsEmailInUse(newEmail, member.Id))
        {
            return (EmailVerificationConfirmResult.InUse, member);
        }

        string previousEmail = member.Email ?? "";
        bool wasInternalEmployee = member.IsInternalEmployee;

        IdentityResult result;
        try
        {
            using var _ = MemberAlternateEmailRules.BeginSelfServiceWrite();

            // Proves they own the new email. Saved only if the change succeeds. Keeps an earlier date.
            member.EmailConfirmedDate ??= DateTime.Now;

            // Verifies the token, sets the email, keeps it confirmed, rotates the security stamp
            result = await userManager.ChangeEmailAsync(member, newEmail, token);
        }
        catch (MemberEmailInUseException)
        {
            // Another member took the email between the check above and the save
            return (EmailVerificationConfirmResult.InUse, member);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(new EventId(0, "MEMBER_EMAIL_CHANGE_FAILED"), ex, "Email change failed for member {MemberID}", memberID);
            return (EmailVerificationConfirmResult.Invalid, member);
        }

        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateEmail))
                ? (EmailVerificationConfirmResult.InUse, member)
                : (EmailVerificationConfirmResult.Invalid, member);
        }

        // The new account email can't stay the alternate email too
        if (EmailsMatch(member.AlternateEmail, newEmail))
        {
            await SetAlternateEmail(member, "", null);
        }

        await RunSafely(() => contactManager.ChangeMemberContactEmail(previousEmail, member), "MEMBER_EMAIL_CHANGE_CONTACT_FAILED", memberID);
        await RunSafely(() => UpdateKenticoEmployeeRole(member, wasInternalEmployee), "MEMBER_EMAIL_CHANGE_ROLE_FAILED", memberID);
        await RunSafely(() => NotifyPreviousEmail(member, previousEmail, newEmail), "MEMBER_EMAIL_CHANGE_NOTIFY_FAILED", memberID);

        return (EmailVerificationConfirmResult.Verified, member);
    }

    /// <summary>
    /// Stores <paramref name="email"/> as the member's unverified alternate email and emails it a link.
    /// </summary>
    public async Task<EmailVerificationRequestResult> RequestAlternateEmail(CommunityMember member, string email)
    {
        email = email.Trim();

        if (EmailsMatch(member.Email, email))
        {
            return EmailVerificationRequestResult.SameAsAccountEmail;
        }

        if (EmailsMatch(member.AlternateEmail, email) && member.IsAlternateEmailVerified)
        {
            return EmailVerificationRequestResult.AlreadyVerified;
        }

        if (await IsEmailInUse(email, member.Id))
        {
            return EmailVerificationRequestResult.InUse;
        }

        await SetAlternateEmail(member, email, null);
        await SendAlternateEmailVerification(member);

        return EmailVerificationRequestResult.Sent;
    }

    public async Task<EmailVerificationRequestResult> ResendAlternateEmailVerification(CommunityMember member)
    {
        if (string.IsNullOrWhiteSpace(member.AlternateEmail))
        {
            return EmailVerificationRequestResult.NotSet;
        }

        if (member.IsAlternateEmailVerified)
        {
            return EmailVerificationRequestResult.AlreadyVerified;
        }

        await SendAlternateEmailVerification(member);

        return EmailVerificationRequestResult.Sent;
    }

    public Task RemoveAlternateEmail(CommunityMember member) =>
        SetAlternateEmail(member, "", null);

    public async Task<EmailVerificationConfirmResult> ConfirmAlternateEmail(int memberID, string email, string token)
    {
        var member = await FindActiveMember(memberID);

        // The member removed or replaced the email after the link was sent
        if (member is null || string.IsNullOrWhiteSpace(token) || !EmailsMatch(member.AlternateEmail, email))
        {
            return EmailVerificationConfirmResult.Invalid;
        }

        if (member.IsAlternateEmailVerified)
        {
            return EmailVerificationConfirmResult.AlreadyVerified;
        }

        if (!await userManager.VerifyUserTokenAsync(member, TokenOptions.DefaultProvider, AlternateEmailTokenPurpose(member.AlternateEmail), token))
        {
            return EmailVerificationConfirmResult.Invalid;
        }

        if (await IsEmailInUse(member.AlternateEmail, member.Id))
        {
            return EmailVerificationConfirmResult.InUse;
        }

        try
        {
            await SetAlternateEmail(member, member.AlternateEmail, DateTime.Now);
        }
        catch (MemberEmailInUseException)
        {
            // Another member took the email between the check above and the save
            return EmailVerificationConfirmResult.InUse;
        }

        return EmailVerificationConfirmResult.Verified;
    }

    /// <summary>
    /// True when another member has <paramref name="email"/> as their account email or verified alternate email.
    /// Unverified alternate emails never count.
    /// </summary>
    public async Task<bool> IsEmailInUse(string email, int excludedMemberID)
    {
        var accountEmailMember = await userManager.FindByEmailAsync(email.Trim());
        if (accountEmailMember is not null && accountEmailMember.Id != excludedMemberID)
        {
            return true;
        }

        return (await GetMembersWithVerifiedAlternateEmail(email)).Any(m => m.MemberID != excludedMemberID);
    }

    /// <summary>
    /// Matches company / integration data (system and data connections) to a member by their account email,
    /// or else by their verified alternate email. Unverified alternate emails never match.
    /// Never use this to authenticate or authorize: it ignores whether the member is enabled or moderated.
    /// </summary>
    /// <remarks>
    /// Deterministic even if the uniqueness rules were bypassed (e.g. direct SQL): see <see cref="SelectIntegrationMatch"/>.
    /// </remarks>
    public async Task<CommunityMember?> FindMemberForIntegrationMatch(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        email = email.Trim();

        var candidates = await memberProvider.Get()
            .Where(new WhereCondition()
                .Where(new WhereCondition().WhereEquals(nameof(MemberInfo.MemberEmail), email))
                .Or()
                .Where(new WhereCondition()
                    .WhereEquals(MemberInfoExtensions.FIELD_ALTERNATE_EMAIL, email)
                    .WhereNotNull(MemberInfoExtensions.FIELD_ALTERNATE_EMAIL_VERIFIED_DATE)))
            .GetEnumerableTypedResultAsync();

        int? memberID = SelectIntegrationMatch(candidates
            .Select(m => new IntegrationMatchCandidate(m.MemberID, EmailsMatch(m.MemberEmail, email))));

        return memberID is int id
            ? CommunityMember.FromMemberInfo(candidates.First(m => m.MemberID == id))
            : null;
    }

    /// <summary>
    /// An account email match wins over a verified alternate email match. Ties: the lowest member ID (oldest member).
    /// </summary>
    public static int? SelectIntegrationMatch(IEnumerable<IntegrationMatchCandidate> candidates) =>
        candidates
            .OrderByDescending(c => c.IsAccountEmailMatch)
            .ThenBy(c => c.MemberID)
            .Select(c => (int?)c.MemberID)
            .FirstOrDefault();

    private async Task<IReadOnlyList<MemberInfo>> GetMembersWithVerifiedAlternateEmail(string email) =>
        [.. await memberProvider.Get()
            .WhereEquals(MemberInfoExtensions.FIELD_ALTERNATE_EMAIL, email.Trim())
            .WhereNotNull(MemberInfoExtensions.FIELD_ALTERNATE_EMAIL_VERIFIED_DATE)
            .GetEnumerableTypedResultAsync()];

    private async Task<CommunityMember?> FindActiveMember(int memberID)
    {
        var member = await userManager.FindByIdAsync(memberID.ToString());

        return member is null || !member.Enabled || member.IsUnderModeration
            ? null
            : member;
    }

    private async Task SendAlternateEmailVerification(CommunityMember member)
    {
        string token = await userManager.GenerateUserTokenAsync(member, TokenOptions.DefaultProvider, AlternateEmailTokenPurpose(member.AlternateEmail));
        string url = GetVerificationURL(nameof(MemberEmailController.ConfirmAlternateEmail), member.Id, member.AlternateEmail, token);

        await emailService.SendEmail(member, MemberEmailConfiguration.AlternateEmailVerification(url), member.AlternateEmail);
    }

    /// <summary>
    /// Writes only the alternate email columns. Saving the whole member (UserManager.UpdateAsync) could
    /// write back stale values, like the security stamp, loaded earlier in the request.
    /// </summary>
    private async Task SetAlternateEmail(CommunityMember member, string email, DateTime? verifiedDate)
    {
        var memberInfo = await memberProvider.GetAsync(member.Id)
            ?? throw new InvalidOperationException($"Member {member.Id} not found");

        memberInfo.MemberAlternateEmail = email;
        memberInfo.MemberAlternateEmailVerifiedDate = verifiedDate;

        using (MemberAlternateEmailRules.BeginSelfServiceWrite())
        {
            await memberProvider.SetAsync(memberInfo);
        }

        member.AlternateEmail = email;
        member.AlternateEmailVerifiedDate = verifiedDate;
    }

    /// <summary>
    /// Confirming a new @kentico.com email grants the role, like registration does (ADR 0003). Moving away from
    /// one revokes it: offboarding matches members by @kentico.com email and could no longer find this member.
    /// </summary>
    public async Task UpdateKenticoEmployeeRole(CommunityMember member, bool wasInternalEmployee)
    {
        string role = SystemMemberRoles.KenticoEmployee.MemberRoleName;
        bool inRole = await userManager.IsInRoleAsync(member, role);

        var result = (member.IsInternalEmployee, wasInternalEmployee, inRole) switch
        {
            (true, _, false) => await userManager.AddToRoleAsync(member, role),
            (false, true, true) => await userManager.RemoveFromRoleAsync(member, role),
            _ => IdentityResult.Success
        };

        if (!result.Succeeded)
        {
            logger.LogError(
                new EventId(0, "MEMBER_EMAIL_CHANGE_ROLE_FAILED"),
                "Failed to update the KenticoEmployee role for member {MemberID}: {Errors}",
                member.Id,
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    private Task NotifyPreviousEmail(CommunityMember member, string previousEmail, string newEmail) =>
        string.IsNullOrWhiteSpace(previousEmail)
            ? Task.CompletedTask
            : emailService.SendEmail(
                member,
                MemberEmailConfiguration.EmailChangedNotification(
                    member.UserName ?? "",
                    MemberEmailConfiguration.MaskEmail(newEmail),
                    GetAbsoluteURL(SUPPORT_PATH)),
                previousEmail);

    private string GetVerificationURL(string action, int memberID, string email, string token)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Verification links can only be created during a request");

        return linkGenerator.GetUriByAction(
            httpContext,
            action,
            "MemberEmail",
            new { memberID, email, token })
            ?? throw new InvalidOperationException($"Could not create the {action} verification link");
    }

    private string GetAbsoluteURL(string path)
    {
        var request = httpContextAccessor.HttpContext?.Request;

        return request is null
            ? path
            : $"{request.Scheme}://{request.Host}{request.PathBase}{path}";
    }

    /// <summary>
    /// Follow-up steps after the email changed: a failure is logged, not shown, because the change itself succeeded.
    /// </summary>
    private async Task RunSafely(Func<Task> action, string eventName, int memberID)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.LogError(new EventId(0, eventName), ex, "Email change follow-up step failed for member {MemberID}", memberID);
        }
    }
}
