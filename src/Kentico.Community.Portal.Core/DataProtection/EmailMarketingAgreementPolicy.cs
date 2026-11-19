namespace Kentico.Community.Portal.Core.DataProtection;

/// <summary>
/// One <c>EmailSubscriptionConfirmationInfo</c> row of the newsletter list. Xperience inserts a new
/// row for every confirm and revoke, so a contact's current state is its latest row.
/// </summary>
public record NewsletterConfirmation(int ConfirmationID, int ContactID, DateTime Date, bool IsApproved);

/// <summary>
/// Contacts whose <see cref="ConsentCodeNames.EMAIL_MARKETING"/> agreement must be added or revoked.
/// </summary>
public record EmailMarketingReconciliation(IReadOnlyList<int> ContactIDsToAgree, IReadOnlyList<int> ContactIDsToRevoke);

/// <summary>
/// What a contact's <see cref="ConsentCodeNames.EMAIL_MARKETING"/> agreement needs, by
/// <see cref="EmailMarketingAgreementPolicy.Decide"/>.
/// </summary>
public enum EmailMarketingAgreementAction
{
    None,
    Agree,
    Revoke,
}

/// <summary>
/// When a newsletter subscription carries an Email marketing agreement.
/// </summary>
public static class EmailMarketingAgreementPolicy
{
    /// <summary>
    /// How long an approved confirmation without list membership counts as still joining.
    /// </summary>
    public static readonly TimeSpan PendingMembershipGrace = TimeSpan.FromMinutes(10);

    /// <summary>
    /// An approved confirmation dated on or after the cutoff. Earlier subscribers never saw the
    /// consent text, so no agreement is recorded for them.
    /// </summary>
    public static bool IsAgreeable(bool isApproved, DateTime confirmationDate, DateTime cutoff) =>
        isApproved && confirmationDate >= cutoff;

    /// <summary>
    /// The latest confirmation, newest by date then ID (the order
    /// <see cref="Membership.RecipientListManager"/> uses), or null when there are none.
    /// </summary>
    public static NewsletterConfirmation? Latest(IEnumerable<NewsletterConfirmation> confirmations) =>
        confirmations
            .OrderByDescending(c => c.Date)
            .ThenByDescending(c => c.ConfirmationID)
            .FirstOrDefault();

    /// <summary>
    /// The latest confirmation per contact, by <see cref="Latest"/>.
    /// </summary>
    public static IReadOnlyDictionary<int, NewsletterConfirmation> LatestByContact(IEnumerable<NewsletterConfirmation> confirmations) =>
        confirmations
            .GroupBy(c => c.ContactID)
            .ToDictionary(g => g.Key, g => Latest(g)!);

    /// <summary>
    /// A contact's agreement from its latest confirmation, never from the row just written alone:
    /// Xperience updates old rows too (a contact merge re-points the merged contact's rows).
    /// <list type="bullet">
    /// <item>No confirmation or latest unapproved → revoke.</item>
    /// <item>Latest approved and <see cref="IsAgreeable"/> → agree.</item>
    /// <item>Latest approved before the cutoff → nothing.</item>
    /// </list>
    /// </summary>
    /// <param name="contactConfirmations">One contact's confirmations of one recipient list.</param>
    /// <param name="cutoff"><see cref="EmailMarketingOptions.AgreementCutoffDate"/>.</param>
    public static EmailMarketingAgreementAction Decide(IEnumerable<NewsletterConfirmation> contactConfirmations, DateTime cutoff) =>
        Latest(contactConfirmations) switch
        {
            null or { IsApproved: false } => EmailMarketingAgreementAction.Revoke,
            var latest when IsAgreeable(latest.IsApproved, latest.Date, cutoff) => EmailMarketingAgreementAction.Agree,
            _ => EmailMarketingAgreementAction.None,
        };

    /// <summary>
    /// Subscribed = list member + latest confirmation approved (what <see cref="Membership.RecipientListManager"/> means).
    /// <list type="bullet">
    /// <item>Subscribed, agreeable and not agreed → agree.</item>
    /// <item>Agreed and not subscribed → revoke, unless the latest confirmation is approved and
    /// newer than <see cref="PendingMembershipGrace"/>: Xperience's double opt-in writes the
    /// confirmation before the list membership, so a run in between must not revoke.</item>
    /// </list>
    /// </summary>
    /// <param name="confirmations">Every newsletter confirmation row.</param>
    /// <param name="listMemberContactIDs">Contacts in the newsletter recipient list.</param>
    /// <param name="agreedContactIDs">Contacts whose latest Email marketing agreement isn't revoked.</param>
    /// <param name="cutoff"><see cref="EmailMarketingOptions.AgreementCutoffDate"/>.</param>
    /// <param name="now">Same clock as the confirmation dates (server local time).</param>
    public static EmailMarketingReconciliation Reconcile(
        IEnumerable<NewsletterConfirmation> confirmations,
        IReadOnlySet<int> listMemberContactIDs,
        IReadOnlySet<int> agreedContactIDs,
        DateTime cutoff,
        DateTime now)
    {
        var latest = LatestByContact(confirmations);

        bool IsSubscribed(int contactID) =>
            listMemberContactIDs.Contains(contactID)
            && latest.TryGetValue(contactID, out var confirmation)
            && confirmation.IsApproved;

        bool IsJoining(int contactID) =>
            latest.TryGetValue(contactID, out var confirmation)
            && confirmation.IsApproved
            && confirmation.Date > now - PendingMembershipGrace;

        var toAgree = latest.Values
            .Where(c => IsSubscribed(c.ContactID)
                && !agreedContactIDs.Contains(c.ContactID)
                && IsAgreeable(c.IsApproved, c.Date, cutoff))
            .Select(c => c.ContactID)
            .Order()
            .ToList();

        var toRevoke = agreedContactIDs
            .Where(id => !IsSubscribed(id) && !IsJoining(id))
            .Order()
            .ToList();

        return new(toAgree, toRevoke);
    }
}
