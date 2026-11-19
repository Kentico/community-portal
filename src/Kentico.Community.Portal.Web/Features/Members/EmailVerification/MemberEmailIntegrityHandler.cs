using CMS.Base;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.Members.EmailVerification;

/// <summary>
/// Another member already holds the email as their account email or verified alternate email.
/// </summary>
public class MemberEmailInUseException(string message) : InvalidOperationException(message);

/// <summary>
/// Rules for the alternate email verified date, outside of member self-service.
/// </summary>
public static class MemberAlternateEmailRules
{
    private static readonly AsyncLocal<int> selfServiceDepth = new();

    /// <summary>
    /// True inside <see cref="BeginSelfServiceWrite"/>: the member verification flow
    /// (<see cref="MemberEmailVerificationService"/>) sets the verified date and runs follow-up steps itself.
    /// </summary>
    public static bool IsSelfServiceWrite => selfServiceDepth.Value > 0;

    public static IDisposable BeginSelfServiceWrite()
    {
        selfServiceDepth.Value++;
        return new SelfServiceScope();
    }

    /// <summary>
    /// Any other write (admin Edit form, Management API, code) is trusted: someone who handled the
    /// member's request vouches for the address. A changed, non-empty alternate email is verified now.
    /// Clearing the email clears the date. An unchanged email (ignoring case and whitespace) keeps its original date.
    /// </summary>
    public static (string Email, DateTime? VerifiedDate) ResolveTrustedWrite(
        string? originalEmail,
        DateTime? originalVerifiedDate,
        string? email,
        DateTime now)
    {
        email = email?.Trim() ?? "";

        if (email.Length == 0)
        {
            return ("", null);
        }

        return MemberEmailVerificationService.EmailsMatch(originalEmail, email)
            ? (email, originalVerifiedDate)
            : (email, now);
    }

    private sealed class SelfServiceScope : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                selfServiceDepth.Value--;
            }
        }
    }
}

/// <summary>
/// Rules for <see cref="CommunityMember.EmailConfirmedDate"/> on every member write.
/// </summary>
public static class MemberEmailConfirmedRules
{
    /// <summary>
    /// A member who becomes enabled (registration confirmation, or an administrator enabling them) without a
    /// confirmed date gets one now: they can sign in, so a later disable must not be undone by re-confirming.
    /// A set date is never changed or cleared here.
    /// </summary>
    public static DateTime? Resolve(bool wasEnabled, bool isEnabled, DateTime? confirmedDate, DateTime now) =>
        confirmedDate ?? (isEnabled && !wasEnabled ? now : null);
}

/// <summary>
/// Data-layer email rules for every <see cref="MemberInfo"/> insert / update (registration, self-service,
/// admin Edit form, Management API):
/// <list type="bullet">
/// <item>An account email or verified alternate email can't be another member's account email or verified alternate email (case-insensitive).</item>
/// <item>Outside self-service: <see cref="MemberAlternateEmailRules.ResolveTrustedWrite"/> sets the verified date.</item>
/// <item>Outside self-service, an account email change rotates the security stamp and re-checks the KenticoEmployee role.</item>
/// <item>A newly verified address is removed from other members' unverified alternate emails.</item>
/// </list>
/// </summary>
public class MemberEmailIntegrityHandler(
    MemberEmailVerificationService verificationService,
    IInfoProvider<MemberInfo> memberProvider,
    TimeProvider timeProvider,
    IServiceScopeFactory scopeFactory,
    ILogger<MemberEmailIntegrityHandler> logger)
{
    private readonly IServiceScopeFactory scopeFactory = scopeFactory;
    private const string FIELD_EMAIL = nameof(MemberInfo.MemberEmail);
    private const string FIELD_SECURITY_STAMP = nameof(MemberInfo.MemberSecurityStamp);
    private const string FIELD_ALTERNATE_EMAIL = MemberInfoExtensions.FIELD_ALTERNATE_EMAIL;
    private const string FIELD_ALTERNATE_EMAIL_VERIFIED_DATE = MemberInfoExtensions.FIELD_ALTERNATE_EMAIL_VERIFIED_DATE;

    private readonly MemberEmailVerificationService verificationService = verificationService;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ILogger<MemberEmailIntegrityHandler> logger = logger;

    /// <exception cref="MemberEmailInUseException">Another member holds the account email or verified alternate email.</exception>
    public async Task HandleBefore(MemberInfo member, bool isInsert, CMSEventArgs args)
    {
        bool isSelfService = MemberAlternateEmailRules.IsSelfServiceWrite;

        string originalEmail = isInsert ? "" : GetOriginalString(member, FIELD_EMAIL);
        string originalAlternateEmail = isInsert ? "" : GetOriginalString(member, FIELD_ALTERNATE_EMAIL);
        var originalVerifiedDate = isInsert ? null : member.GetOriginalValue(FIELD_ALTERNATE_EMAIL_VERIFIED_DATE) as DateTime?;

        bool wasEnabled = !isInsert && (member.GetOriginalValue(nameof(MemberInfo.MemberEnabled)) as bool? ?? false);
        var confirmedDate = MemberEmailConfirmedRules.Resolve(
            wasEnabled,
            member.MemberEnabled,
            member.MemberEmailConfirmedDate,
            timeProvider.GetLocalNow().DateTime);
        if (confirmedDate != member.MemberEmailConfirmedDate)
        {
            member.MemberEmailConfirmedDate = confirmedDate;
        }

        string email = member.MemberEmail?.Trim() ?? "";
        bool emailChanged = isInsert || !string.Equals(originalEmail, email, StringComparison.Ordinal);

        // The account email can't also be the alternate email
        if (MemberEmailVerificationService.EmailsMatch(email, member.MemberAlternateEmail))
        {
            member.MemberAlternateEmail = "";
        }

        if (isSelfService)
        {
            if (string.IsNullOrWhiteSpace(member.MemberAlternateEmail))
            {
                member.MemberAlternateEmailVerifiedDate = null;
            }
        }
        else
        {
            var (alternateEmail, verifiedDate) = MemberAlternateEmailRules.ResolveTrustedWrite(
                originalAlternateEmail,
                originalVerifiedDate,
                member.MemberAlternateEmail,
                timeProvider.GetLocalNow().DateTime);

            member.MemberAlternateEmail = alternateEmail;
            member.MemberAlternateEmailVerifiedDate = verifiedDate;
        }

        string verifiedAlternateEmail = member.MemberAlternateEmailVerifiedDate is null ? "" : member.MemberAlternateEmail;
        bool alternateEmailNewlyVerified = verifiedAlternateEmail.Length > 0
            && (isInsert
                || originalVerifiedDate is null
                || !MemberEmailVerificationService.EmailsMatch(originalAlternateEmail, verifiedAlternateEmail));

        var newlyVerified = new List<string>();

        if (emailChanged && email.Length > 0)
        {
            await EnsureNotInUse(email, member.MemberID, "account email");
            newlyVerified.Add(email);
        }

        if (alternateEmailNewlyVerified)
        {
            await EnsureNotInUse(verifiedAlternateEmail, member.MemberID, "alternate email");
            newlyVerified.Add(verifiedAlternateEmail);
        }

        bool accountEmailChangedByTrustedWrite = !isInsert && !isSelfService && emailChanged
            && !string.Equals(originalEmail, email, StringComparison.OrdinalIgnoreCase);

        if (accountEmailChangedByTrustedWrite && !member.ItemChanged(FIELD_SECURITY_STAMP))
        {
            member.SetValue(FIELD_SECURITY_STAMP, Guid.NewGuid().ToString("N").ToUpperInvariant());
        }

        bool wasInternalEmployee = accountEmailChangedByTrustedWrite && WasInternalEmployee(member, originalEmail);

        if (newlyVerified.Count == 0 && !accountEmailChangedByTrustedWrite)
        {
            return;
        }

        /*
         * Only once the member is saved (a failed save must not change other data), and in the background:
         * the save can still be inside a transaction holding a lock on the member row, and these steps query
         * and update CMS_Member on other connections. Run inline, two concurrent saves wait on each other's
         * locks until the SQL command timeout. In the background they wait for the commit instead.
         * The follow-up gets its own service scope and doesn't inherit the save's connection context.
         */
        args.CallWhenFinished(() =>
        {
            using (ExecutionContext.SuppressFlow())
            {
                _ = Task.Run(async () =>
                {
                    using var scope = scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<MemberEmailIntegrityHandler>()
                        .RunFollowUp(member, newlyVerified, accountEmailChangedByTrustedWrite, wasInternalEmployee);
                });
            }
        });
    }

    private async Task RunFollowUp(MemberInfo member, IReadOnlyList<string> newlyVerified, bool accountEmailChanged, bool wasInternalEmployee)
    {
        await RunSafely(() => ClearUnverifiedAlternateEmails(newlyVerified, member.MemberID), "MEMBER_EMAIL_CLEAR_UNVERIFIED_FAILED", member.MemberID);

        if (accountEmailChanged)
        {
            await RunSafely(
                () => verificationService.UpdateKenticoEmployeeRole(CommunityMember.FromMemberInfo(member), wasInternalEmployee),
                "MEMBER_EMAIL_CHANGE_ROLE_FAILED",
                member.MemberID);
        }
    }

    private async Task EnsureNotInUse(string email, int memberID, string label)
    {
        if (await verificationService.IsEmailInUse(email, memberID))
        {
            throw new MemberEmailInUseException($"The {label} is already used by another member as their account email or verified alternate email.");
        }
    }

    /// <summary>
    /// Unverified alternate emails never count toward uniqueness, so they are dropped once another
    /// member proves they own the address. Those members can't verify it anymore anyway.
    /// </summary>
    private async Task ClearUnverifiedAlternateEmails(IEnumerable<string> emails, int memberID)
    {
        foreach (string email in emails)
        {
            var members = await memberProvider.Get()
                .WhereEquals(FIELD_ALTERNATE_EMAIL, email)
                .WhereNull(FIELD_ALTERNATE_EMAIL_VERIFIED_DATE)
                .WhereNotEquals(nameof(MemberInfo.MemberID), memberID)
                .GetEnumerableTypedResultAsync();

            foreach (var other in members)
            {
                other.MemberAlternateEmail = "";
                other.MemberAlternateEmailVerifiedDate = null;
                await memberProvider.SetAsync(other);
            }
        }
    }

    private static bool WasInternalEmployee(MemberInfo member, string originalEmail)
    {
        var original = CommunityMember.FromMemberInfo(member);
        original.Email = originalEmail;
        original.EmailConfirmed = member.GetOriginalValue(nameof(MemberInfo.MemberEnabled)) as bool? ?? member.MemberEnabled;

        return original.IsInternalEmployee;
    }

    private static string GetOriginalString(MemberInfo member, string column) =>
        (member.GetOriginalValue(column) as string)?.Trim() ?? "";

    private async Task RunSafely(Func<Task> action, string eventName, int memberID)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.LogError(new EventId(0, eventName), ex, "Member email follow-up step failed for member {MemberID}", memberID);
        }
    }
}
