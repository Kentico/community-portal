using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.EmailMarketing;

namespace Kentico.Community.Portal.Web.Tests.Fixtures;

/// <summary>
/// Builds a contact plus the consent agreements and recipient list subscriptions the consent and
/// retention code reads. Without <see cref="WithEmail" /> the contact is anonymous, like one
/// created by tracking.
/// </summary>
public class ContactBuilder(PrivacyTestData data)
{
    private readonly PrivacyTestData data = data;

    private string? email;
    private string? firstName;
    private string? lastName;
    private DateTime? created;
    private readonly List<(ConsentInfo Consent, bool Revoked, DateTime? At)> agreements = [];
    private readonly List<(ContactGroupInfo List, bool Approved, DateTime? At)> subscriptions = [];

    public ContactBuilder WithEmail(string? contactEmail = null)
    {
        email = contactEmail ?? data.UniqueEmail();
        return this;
    }

    public ContactBuilder WithName(string first, string last)
    {
        firstName = first;
        lastName = last;
        return this;
    }

    public ContactBuilder CreatedAt(DateTime createdAt)
    {
        created = createdAt;
        return this;
    }

    /// <summary>
    /// Agrees through <see cref="IConsentAgreementService" />, so the stored hash matches the
    /// consent's current text. <paramref name="at" /> backdates the agreement row.
    /// </summary>
    public ContactBuilder AgreedTo(ConsentInfo consent, DateTime? at = null)
    {
        agreements.Add((consent, false, at));
        return this;
    }

    /// <summary>
    /// Agrees, then revokes, leaving the two rows Xperience writes for a withdrawn consent.
    /// The agreement is dated a minute before the revocation: rows written in the same instant
    /// tie, and Xperience may then read the agreement as the latest.
    /// </summary>
    public ContactBuilder RevokedFrom(ConsentInfo consent, DateTime? at = null)
    {
        var revokedAt = at ?? DateTime.Now;

        agreements.Add((consent, false, revokedAt.AddMinutes(-1)));
        agreements.Add((consent, true, revokedAt));
        return this;
    }

    /// <summary>
    /// Adds a list membership and a subscription confirmation, the same two rows
    /// <c>RecipientListManager.Subscribe</c> writes. <paramref name="approved" /> false models a
    /// pending double opt-in.
    /// </summary>
    public ContactBuilder SubscribedTo(ContactGroupInfo recipientList, bool approved = true, DateTime? at = null)
    {
        subscriptions.Add((recipientList, approved, at));
        return this;
    }

    public ContactInfo Build()
    {
        var contactProvider = Service.Resolve<IInfoProvider<ContactInfo>>();

        var contact = new ContactInfo
        {
            ContactEmail = email ?? "",
            ContactFirstName = firstName ?? "",
            ContactLastName = lastName ?? email ?? data.UniqueName("AnonymousContact"),
        };

        using (PrivacyTestData.WriteContext())
        {
            contactProvider.Set(contact);
            data.Track(contact);

            if (created is DateTime createdAt)
            {
                // Set after insert because the insert stamps its own creation time
                contact.ContactCreated = createdAt;
                contactProvider.Set(contact);
            }

            foreach (var (consent, revoked, at) in agreements)
            {
                Agree(contact, consent, revoked, at);
            }

            foreach (var (list, approved, at) in subscriptions)
            {
                Subscribe(contact, list, approved, at);
            }
        }

        return contact;
    }

    private static void Agree(ContactInfo contact, ConsentInfo consent, bool revoke, DateTime? at)
    {
        var service = Service.Resolve<IConsentAgreementService>();

        var agreement = revoke
            ? service.Revoke(contact, consent)
            : service.Agree(contact, consent);

        if (at is DateTime agreedAt)
        {
            agreement.ConsentAgreementTime = agreedAt;
            Service.Resolve<IInfoProvider<ConsentAgreementInfo>>().Set(agreement);
        }
    }

    private static void Subscribe(ContactInfo contact, ContactGroupInfo list, bool approved, DateTime? at)
    {
        Service.Resolve<IInfoProvider<ContactGroupMemberInfo>>().Set(new ContactGroupMemberInfo
        {
            ContactGroupMemberContactGroupID = list.ContactGroupID,
            ContactGroupMemberType = ContactGroupMemberTypeEnum.Contact,
            ContactGroupMemberRelatedID = contact.ContactID,
            ContactGroupMemberFromManual = true,
            ContactGroupMemberFromCondition = false,
        });

        Service.Resolve<IInfoProvider<EmailSubscriptionConfirmationInfo>>().Set(new EmailSubscriptionConfirmationInfo
        {
            EmailSubscriptionConfirmationContactID = contact.ContactID,
            EmailSubscriptionConfirmationRecipientListID = list.ContactGroupID,
            EmailSubscriptionConfirmationIsApproved = approved,
            EmailSubscriptionConfirmationDate = at ?? DateTime.UtcNow,
        });
    }
}
