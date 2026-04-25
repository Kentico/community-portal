using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.EmailMarketing;

namespace Kentico.Community.Portal.Core.Membership;

public class RecipientListManager(
    IInfoProvider<ContactGroupInfo> contactGroupProvider,
    IInfoProvider<ContactInfo> contactProvider,
    IInfoProvider<ContactGroupMemberInfo> contactGroupMemberProvider,
    IInfoProvider<EmailSubscriptionConfirmationInfo> subscriptionConfirmationProvider,
    IReadOnlyModeProvider readOnlyProvider)
{
    private readonly IInfoProvider<ContactGroupInfo> contactGroupProvider = contactGroupProvider;
    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;
    private readonly IInfoProvider<ContactGroupMemberInfo> contactGroupMemberProvider = contactGroupMemberProvider;
    private readonly IInfoProvider<EmailSubscriptionConfirmationInfo> subscriptionConfirmationProvider = subscriptionConfirmationProvider;
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
    public async Task<IReadOnlyList<int>> GetSubscribedRecipientListIDs(string memberEmail)
    {
        var contact = (await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), memberEmail)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (contact is null)
        {
            return [];
        }

        var confirmedRecipientListSubquery = subscriptionConfirmationProvider.Get()
            .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID), contact.ContactID)
            .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationIsApproved), true)
            .Column(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationRecipientListID));

        var memberships = await contactGroupMemberProvider.Get()
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contact.ContactID)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
            .WhereIn(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID), confirmedRecipientListSubquery)
            .Column(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID))
            .GetEnumerableTypedResultAsync();

        return [.. memberships.Select(m => m.ContactGroupMemberContactGroupID)];
    }

    /// <summary>
    /// Subscribes the member's contact to the specified recipient list.
    /// Does not require double opt-in since the member is already authenticated with a confirmed email.
    /// </summary>
    public async Task Subscribe(string memberEmail, int recipientListID)
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

        var contact = await GetOrCreateContactByEmail(memberEmail);

        bool alreadyMember = (await contactGroupMemberProvider.Get()
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID), recipientListID)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contact.ContactID)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
            .GetEnumerableTypedResultAsync())
            .Any();

        if (alreadyMember)
        {
            return;
        }

        using var context = new CMSActionContext { LogEvents = false };

        var groupMember = new ContactGroupMemberInfo
        {
            ContactGroupMemberContactGroupID = recipientListID,
            ContactGroupMemberType = ContactGroupMemberTypeEnum.Contact,
            ContactGroupMemberRelatedID = contact.ContactID,
            ContactGroupMemberFromManual = true,
            ContactGroupMemberFromCondition = false,
        };
        contactGroupMemberProvider.Set(groupMember);

        var confirmation = new EmailSubscriptionConfirmationInfo
        {
            EmailSubscriptionConfirmationContactID = contact.ContactID,
            EmailSubscriptionConfirmationRecipientListID = recipientListID,
            EmailSubscriptionConfirmationIsApproved = true,
            EmailSubscriptionConfirmationDate = DateTime.UtcNow,
        };
        subscriptionConfirmationProvider.Set(confirmation);
    }

    /// <summary>
    /// Unsubscribes the member's contact from the specified recipient list.
    /// </summary>
    public async Task Unsubscribe(string memberEmail, int recipientListID)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return;
        }

        var contact = (await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), memberEmail)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (contact is null)
        {
            return;
        }

        using var context = new CMSActionContext { LogEvents = false };

        var membership = (await contactGroupMemberProvider.Get()
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID), recipientListID)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contact.ContactID)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (membership is not null)
        {
            contactGroupMemberProvider.Delete(membership);
        }

        var confirmation = (await subscriptionConfirmationProvider.Get()
            .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID), contact.ContactID)
            .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationRecipientListID), recipientListID)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (confirmation is not null)
        {
            subscriptionConfirmationProvider.Delete(confirmation);
        }
    }

    private async Task<ContactInfo> GetOrCreateContactByEmail(string email)
    {
        var existing = (await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), email)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (existing is not null)
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
