using Kentico.Community.Portal.Core.DataProtection;

namespace Kentico.Community.Portal.Web.Tests.Features.Newsletter;

/// <summary>
/// Which newsletter subscriptions carry an Email marketing agreement: only confirmations
/// on/after the cutoff, never pre-cutoff subscribers, and every agreement of a contact that's no
/// longer subscribed is revoked.
/// </summary>
public class EmailMarketingAgreementPolicyTests
{
    private static readonly DateTime Cutoff = new(2026, 10, 1);
    private static readonly DateTime Now = Cutoff.AddDays(30);

    [Test]
    public void IsAgreeable_Needs_Approved_Confirmation_On_Or_After_Cutoff()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EmailMarketingAgreementPolicy.IsAgreeable(true, Cutoff, Cutoff), Is.True);
            Assert.That(EmailMarketingAgreementPolicy.IsAgreeable(true, Cutoff.AddDays(3), Cutoff), Is.True);
            Assert.That(EmailMarketingAgreementPolicy.IsAgreeable(true, Cutoff.AddSeconds(-1), Cutoff), Is.False);
            Assert.That(EmailMarketingAgreementPolicy.IsAgreeable(false, Cutoff.AddDays(3), Cutoff), Is.False);
        });
    }

    [Test]
    public void Reconcile_Agrees_Subscribed_Post_Cutoff_Contacts_Without_Agreement()
    {
        NewsletterConfirmation[] confirmations =
        [
            new(1, ContactID: 10, Cutoff.AddDays(1), IsApproved: true),
            new(2, ContactID: 11, Cutoff.AddDays(1), IsApproved: true),
            new(3, ContactID: 12, Cutoff.AddDays(-30), IsApproved: true),
            new(4, ContactID: 13, Cutoff.AddDays(1), IsApproved: true),
        ];

        var plan = EmailMarketingAgreementPolicy.Reconcile(
            confirmations,
            listMemberContactIDs: new HashSet<int> { 10, 11, 12 },
            agreedContactIDs: new HashSet<int> { 11 },
            Cutoff,
            Now);

        Assert.Multiple(() =>
        {
            // 11 already agreed, 12 is pre-cutoff, 13 isn't a list member (pending double opt-in)
            Assert.That(plan.ContactIDsToAgree, Is.EqualTo(new[] { 10 }));
            Assert.That(plan.ContactIDsToRevoke, Is.Empty);
        });
    }

    [Test]
    public void Reconcile_Revokes_Agreements_Of_Contacts_No_Longer_Subscribed()
    {
        NewsletterConfirmation[] confirmations =
        [
            new(1, ContactID: 20, Cutoff.AddDays(1), IsApproved: true),
            // Xperience's revoke inserts an unapproved row; the latest row counts
            new(2, ContactID: 21, Cutoff.AddDays(1), IsApproved: true),
            new(3, ContactID: 21, Cutoff.AddDays(2), IsApproved: false),
            new(4, ContactID: 22, Cutoff.AddDays(1), IsApproved: true),
        ];

        var plan = EmailMarketingAgreementPolicy.Reconcile(
            confirmations,
            // 22 was removed from the list, 23 has an agreement but no confirmation at all
            listMemberContactIDs: new HashSet<int> { 20, 21 },
            agreedContactIDs: new HashSet<int> { 20, 21, 22, 23 },
            Cutoff,
            Now);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ContactIDsToAgree, Is.Empty);
            Assert.That(plan.ContactIDsToRevoke, Is.EqualTo(new[] { 21, 22, 23 }));
        });
    }

    [Test]
    public void Reconcile_Resubscribed_After_Cutoff_Is_Agreed()
    {
        NewsletterConfirmation[] confirmations =
        [
            new(1, ContactID: 40, Cutoff.AddDays(-100), IsApproved: true),
            new(2, ContactID: 40, Cutoff.AddDays(-50), IsApproved: false),
            new(3, ContactID: 40, Cutoff.AddDays(5), IsApproved: true),
        ];

        var plan = EmailMarketingAgreementPolicy.Reconcile(
            confirmations,
            listMemberContactIDs: new HashSet<int> { 40 },
            agreedContactIDs: new HashSet<int>(),
            Cutoff,
            Now);

        Assert.That(plan.ContactIDsToAgree, Is.EqualTo(new[] { 40 }));
    }

    [Test]
    public void Reconcile_Does_Not_Revoke_A_Fresh_Confirmation_Still_Joining_The_List()
    {
        // Xperience's double opt-in writes the confirmation before the list membership
        NewsletterConfirmation[] confirmations =
        [
            new(1, ContactID: 60, Now.AddMinutes(-1), IsApproved: true),
            new(2, ContactID: 61, Now.AddMinutes(-30), IsApproved: true),
            new(3, ContactID: 62, Now.AddMinutes(-1), IsApproved: false),
        ];

        var plan = EmailMarketingAgreementPolicy.Reconcile(
            confirmations,
            listMemberContactIDs: new HashSet<int>(),
            agreedContactIDs: new HashSet<int> { 60, 61, 62 },
            Cutoff,
            Now);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ContactIDsToAgree, Is.Empty);
            Assert.That(plan.ContactIDsToRevoke, Is.EqualTo(new[] { 61, 62 }));
        });
    }

    [Test]
    public void LatestByContact_Breaks_Date_Ties_By_ID()
    {
        NewsletterConfirmation[] confirmations =
        [
            new(5, ContactID: 50, Cutoff, IsApproved: true),
            new(6, ContactID: 50, Cutoff, IsApproved: false),
        ];

        var latest = EmailMarketingAgreementPolicy.LatestByContact(confirmations);

        Assert.That(latest[50].ConfirmationID, Is.EqualTo(6));
    }

    [Test]
    public void Decide_Agrees_When_Latest_Is_Approved_After_An_Older_Revoke()
    {
        // Listed newest first: the decision must not depend on row order, e.g. the row a merge just updated
        NewsletterConfirmation[] confirmations =
        [
            new(2, ContactID: 70, Cutoff.AddDays(2), IsApproved: true),
            new(1, ContactID: 70, Cutoff.AddDays(1), IsApproved: false),
        ];

        var action = EmailMarketingAgreementPolicy.Decide(confirmations, Cutoff);

        Assert.That(action, Is.EqualTo(EmailMarketingAgreementAction.Agree));
    }

    [Test]
    public void Decide_Revokes_When_Latest_Is_Unapproved_After_An_Older_Approval()
    {
        NewsletterConfirmation[] confirmations =
        [
            new(2, ContactID: 71, Cutoff.AddDays(2), IsApproved: false),
            new(1, ContactID: 71, Cutoff.AddDays(1), IsApproved: true),
        ];

        var action = EmailMarketingAgreementPolicy.Decide(confirmations, Cutoff);

        Assert.That(action, Is.EqualTo(EmailMarketingAgreementAction.Revoke));
    }

    [Test]
    public void Decide_Does_Nothing_When_Latest_Is_Approved_Before_Cutoff()
    {
        NewsletterConfirmation[] confirmations =
        [
            new(1, ContactID: 72, Cutoff.AddDays(-10), IsApproved: false),
            new(2, ContactID: 72, Cutoff.AddDays(-5), IsApproved: true),
        ];

        Assert.Multiple(() =>
        {
            Assert.That(EmailMarketingAgreementPolicy.Decide(confirmations, Cutoff), Is.EqualTo(EmailMarketingAgreementAction.None));
        });
    }

    [Test]
    public void Decide_Revokes_Without_Confirmations()
    {
        var action = EmailMarketingAgreementPolicy.Decide([], Cutoff);

        Assert.That(action, Is.EqualTo(EmailMarketingAgreementAction.Revoke));
    }

    [Test]
    public void Decide_Breaks_Date_Ties_By_Highest_ID()
    {
        // Same order as RecipientListManager.GetLatestConfirmation: date desc, then ID desc
        NewsletterConfirmation[] approvedLast =
        [
            new(7, ContactID: 73, Cutoff.AddDays(1), IsApproved: false),
            new(8, ContactID: 73, Cutoff.AddDays(1), IsApproved: true),
        ];
        NewsletterConfirmation[] revokedLast =
        [
            new(8, ContactID: 74, Cutoff.AddDays(1), IsApproved: false),
            new(7, ContactID: 74, Cutoff.AddDays(1), IsApproved: true),
        ];

        Assert.Multiple(() =>
        {
            Assert.That(EmailMarketingAgreementPolicy.Decide(approvedLast, Cutoff), Is.EqualTo(EmailMarketingAgreementAction.Agree));
            Assert.That(EmailMarketingAgreementPolicy.Decide(revokedLast, Cutoff), Is.EqualTo(EmailMarketingAgreementAction.Revoke));
        });
    }
}
