using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.EmailMarketing;
using CMS.EmailMarketing.Internal;
using CMS.Helpers;
using Kentico.Community.Portal.Core.DataProtection;

namespace Kentico.Community.Portal.Core.Membership;

/// <summary>
/// A recipient list the contact is subscribed to, and when the subscription was confirmed.
/// </summary>
public record RecipientListSubscription(int RecipientListID, DateTime ConfirmedDate);

/// <summary>
/// Subscribes and unsubscribes members' email contacts. Subscriptions to the
/// <see cref="RecipientListCodeNames.NEWSLETTER"/> list also agree or revoke the
/// <see cref="ConsentCodeNames.EMAIL_MARKETING"/> consent inline. Read-only mode: no-op.
/// </summary>
public class RecipientListManager(
    IInfoProvider<ContactGroupInfo> contactGroupProvider,
    IInfoProvider<ContactInfo> contactProvider,
    IInfoProvider<ContactGroupMemberInfo> contactGroupMemberProvider,
    IInfoProvider<EmailSubscriptionConfirmationInfo> subscriptionConfirmationProvider,
    IEmailSubscriptionConfirmationService subscriptionConfirmationService,
    EmailMarketingConsentManager emailMarketingConsentManager,
    IReadOnlyModeProvider readOnlyProvider)
{
    private readonly IInfoProvider<ContactGroupInfo> contactGroupProvider = contactGroupProvider;
    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;
    private readonly IInfoProvider<ContactGroupMemberInfo> contactGroupMemberProvider = contactGroupMemberProvider;
    private readonly IInfoProvider<EmailSubscriptionConfirmationInfo> subscriptionConfirmationProvider = subscriptionConfirmationProvider;
    private readonly IEmailSubscriptionConfirmationService subscriptionConfirmationService = subscriptionConfirmationService;
    private readonly EmailMarketingConsentManager emailMarketingConsentManager = emailMarketingConsentManager;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    /// <summary>
    /// Returns all recipient lists available in the system.
    /// </summary>
    public async Task<IReadOnlyList<ContactGroupInfo>> GetRecipientLists() =>
        [.. await contactGroupProvider.Get()
            .WhereEquals(nameof(ContactGroupInfo.ContactGroupIsRecipientList), true)
            .OrderBy(nameof(ContactGroupInfo.ContactGroupDisplayName))
            .GetEnumerableTypedResultAsync()];

    /// <summary>
    /// Returns the IDs of all recipient lists the given contact is subscribed to.
    /// </summary>
    public async Task<IReadOnlyList<int>> GetSubscribedRecipientListIDs(string memberEmail) =>
        [.. (await GetSubscriptions(memberEmail)).Select(s => s.RecipientListID)];

    /// <summary>
    /// Returns the recipient lists the member's contact is subscribed to, by <see cref="ActiveSubscriptionSql"/>.
    /// </summary>
    public async Task<IReadOnlyList<RecipientListSubscription>> GetSubscriptions(string memberEmail)
    {
        var contact = await GetContactByEmail(memberEmail);
        if (contact is null)
        {
            return [];
        }

        var rows = await contactGroupMemberProvider.Get()
            .Source(s => s.InnerJoin<EmailSubscriptionConfirmationInfo>(
                $"{ActiveSubscriptionSql.MEMBER_TABLE}.{nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID)}",
                nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID)))
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contact.ContactID)
            .Where(new WhereCondition(ActiveSubscriptionSql.Predicate()))
            .Columns(
                nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID),
                nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationDate))
            .GetDataContainerResultAsync();

        return [.. rows.Select(r => new RecipientListSubscription(
            ValidationHelper.GetInteger(r.GetValue(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID)), 0),
            ValidationHelper.GetDateTime(r.GetValue(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationDate)), default)))];
    }

    /// <summary>
    /// Subscribes the member's contact to the specified recipient list.
    /// Does not require double opt-in since the member is already authenticated with a confirmed email.
    /// For the newsletter list, also agrees to the Email marketing consent: the member saw its text
    /// where they asked to subscribe (account page, registration).
    /// </summary>
    public async Task Subscribe(string memberEmail, int recipientListID)
    {
        if (readOnlyProvider.IsReadOnly || string.IsNullOrWhiteSpace(memberEmail))
        {
            return;
        }

        var recipientList = await contactGroupProvider.GetAsync(recipientListID);
        if (recipientList is null || !recipientList.ContactGroupIsRecipientList)
        {
            return;
        }

        var contact = await GetOrCreateContactByEmail(memberEmail);

        bool alreadyMember = await IsListMember(contact.ContactID, recipientListID);
        bool isApproved = (await GetLatestConfirmation(contact.ContactID, recipientListID))?.EmailSubscriptionConfirmationIsApproved ?? false;

        using (new CMSActionContext { LogEvents = false })
        {
            if (!alreadyMember)
            {
                contactGroupMemberProvider.Set(new ContactGroupMemberInfo
                {
                    ContactGroupMemberContactGroupID = recipientListID,
                    ContactGroupMemberType = ContactGroupMemberTypeEnum.Contact,
                    ContactGroupMemberRelatedID = contact.ContactID,
                    ContactGroupMemberFromManual = true,
                    ContactGroupMemberFromCondition = false,
                });
            }

            if (!isApproved)
            {
                subscriptionConfirmationProvider.Set(new EmailSubscriptionConfirmationInfo
                {
                    EmailSubscriptionConfirmationContactID = contact.ContactID,
                    EmailSubscriptionConfirmationRecipientListID = recipientListID,
                    EmailSubscriptionConfirmationIsApproved = true,
                    EmailSubscriptionConfirmationDate = DateTime.Now,
                });
            }
        }

        if (IsNewsletter(recipientList))
        {
            _ = await emailMarketingConsentManager.Agree(contact);
        }
    }

    /// <summary>
    /// Marks the member's contact unsubscribed from the specified recipient list the way Xperience's
    /// email unsubscribe link does: an unapproved confirmation row, the contact stays in the list and
    /// earlier rows stay as the record. Xperience doesn't send to a list member whose latest row is
    /// unapproved. For the newsletter list, also revokes the Email marketing agreement.
    /// </summary>
    public async Task Unsubscribe(string memberEmail, int recipientListID)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return;
        }

        var recipientList = await contactGroupProvider.GetAsync(recipientListID);
        if (recipientList is null || !recipientList.ContactGroupIsRecipientList)
        {
            return;
        }

        var contact = await GetContactByEmail(memberEmail);
        if (contact is null)
        {
            return;
        }

        /*
         * Same calls as Xperience's unsubscribe link (KenticoEmailUnsubscriptionController).
         * IEmailSubscriptionConfirmationService is in CMS.EmailMarketing.Internal, so it can change on
         * Xperience upgrades. IsConfirmed is false for a list member with no rows, but Xperience sends
         * to them, so they're revoked too
         */
        bool shouldRevoke = subscriptionConfirmationService.IsConfirmed(contact, recipientList)
            || (await GetLatestConfirmation(contact.ContactID, recipientListID) is null
                && await IsListMember(contact.ContactID, recipientListID));

        if (shouldRevoke)
        {
            using (new CMSActionContext { LogEvents = false })
            {
                subscriptionConfirmationService.Revoke(contact, recipientList);
            }
        }

        if (IsNewsletter(recipientList))
        {
            _ = await emailMarketingConsentManager.Revoke(contact);
        }
    }

    private static bool IsNewsletter(ContactGroupInfo recipientList) =>
        string.Equals(recipientList.ContactGroupName, RecipientListCodeNames.NEWSLETTER, StringComparison.OrdinalIgnoreCase);

    private async Task<EmailSubscriptionConfirmationInfo?> GetLatestConfirmation(int contactID, int recipientListID) =>
        (await subscriptionConfirmationProvider.Get()
            .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID), contactID)
            .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationRecipientListID), recipientListID)
            .OrderByDescending(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationDate), nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationID))
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

    private async Task<bool> IsListMember(int contactID, int recipientListID) =>
        (await contactGroupMemberProvider.Get()
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID), recipientListID)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contactID)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .Any();

    /// <summary>
    /// The newest contact with the email, or null. Same as <c>MemberContactManager.GetContactByEmail</c>:
    /// API-created contacts aren't merged, so several can share an email.
    /// </summary>
    private async Task<ContactInfo?> GetContactByEmail(string email) =>
        string.IsNullOrWhiteSpace(email)
            ? null
            : (await contactProvider.Get()
                .WhereEquals(nameof(ContactInfo.ContactEmail), email)
                .OrderByDescending(nameof(ContactInfo.ContactCreated), nameof(ContactInfo.ContactID))
                .TopN(1)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();

    private async Task<ContactInfo> GetOrCreateContactByEmail(string email)
    {
        if (await GetContactByEmail(email) is ContactInfo existing)
        {
            return existing;
        }

        var contact = new ContactInfo
        {
            ContactEmail = email,
            ContactLastName = email,
        };

        await contactProvider.SetAsync(contact);

        return contact;
    }
}
