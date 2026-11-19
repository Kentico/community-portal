using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Web.Mvc;
using CookieLevel = Kentico.Web.Mvc.CookieLevel;

namespace Kentico.Community.Portal.Web.Membership;

public class MemberContactManager(
    IInfoProvider<ContactInfo> contactProvider,
    ICurrentContactProvider currentContactProvider,
    ICurrentCookieLevelProvider cookieLevelProvider,
    IContactMergeService contactMerge,
    IReadOnlyModeProvider readOnlyProvider)
{
    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;
    private readonly ICurrentContactProvider currentContactProvider = currentContactProvider;
    private readonly ICurrentCookieLevelProvider cookieLevelProvider = cookieLevelProvider;
    private readonly IContactMergeService contactMerge = contactMerge;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    /// <summary>
    /// Syncs the member's details to their contact.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tracked visitor (<see cref="CookieLevel.All"/>): the browser's contact gets the member's email
    /// and is merged with any contact that already has it, then becomes the current contact. Without
    /// a browser contact, the member's email contact becomes current.
    /// </para>
    /// <para>
    /// Untracked visitor: the contact found by member email is updated (or created). The browser
    /// gets no contact cookie.
    /// </para>
    /// </remarks>
    public async Task<ContactInfo?> SetMemberAsCurrentContact(CommunityMember member)
    {
        if (readOnlyProvider.IsReadOnly || string.IsNullOrWhiteSpace(member.Email))
        {
            return null;
        }

        if (cookieLevelProvider.GetCurrentCookieLevel() < CookieLevel.All.Level)
        {
            var emailContact = await GetContactByEmail(member.Email) ?? new ContactInfo { ContactEmail = member.Email };
            MapMember(member, emailContact);
            await contactProvider.SetAsync(emailContact);

            return emailContact;
        }

        var contact = ContactManagementContext.GetCurrentContact(createAnonymous: false);

        // Another person's contact (e.g. a shared browser): never overwrite its email
        if (contact is not null
            && !string.IsNullOrWhiteSpace(contact.ContactEmail)
            && !string.Equals(contact.ContactEmail, member.Email, StringComparison.OrdinalIgnoreCase))
        {
            contact = null;
        }

        contact ??= await GetContactByEmail(member.Email) ?? new ContactInfo { ContactEmail = member.Email };

        MapMember(member, contact);

        if (!string.Equals(contact.ContactEmail, member.Email, StringComparison.OrdinalIgnoreCase))
        {
            contact.ContactEmail = member.Email;
            await contactProvider.SetAsync(contact);
            contactMerge.MergeContactByEmail(contact);

            // The merge can fold this contact into an older one with the same email
            contact = await contactProvider.GetAsync(contact.ContactID)
                ?? await GetContactByEmail(member.Email)
                ?? contact;
        }
        else
        {
            await contactProvider.SetAsync(contact);
        }

        currentContactProvider.SetCurrentContact(contact);

        return contact;
    }

    /// <summary>
    /// After a member's email changed: moves their contact (the newest with <paramref name="previousEmail"/>),
    /// with its consents and recipient list subscriptions, to the new email, and merges it with any contact
    /// that already has the new email.
    /// </summary>
    public async Task ChangeMemberContactEmail(string previousEmail, CommunityMember member)
    {
        if (readOnlyProvider.IsReadOnly || string.IsNullOrWhiteSpace(member.Email))
        {
            return;
        }

        var contact = await GetContactByEmail(previousEmail);
        if (contact is null)
        {
            return;
        }

        contact.ContactEmail = member.Email;
        MapMember(member, contact);
        await contactProvider.SetAsync(contact);
        contactMerge.MergeContactByEmail(contact);
    }

    /// <summary>
    /// The newest contact with the email, or null.
    /// </summary>
    public async Task<ContactInfo?> GetContactByEmail(string email) =>
        string.IsNullOrWhiteSpace(email)
            ? null
            : (await contactProvider.Get()
                .WhereEquals(nameof(ContactInfo.ContactEmail), email)
                .OrderByDescending(nameof(ContactInfo.ContactCreated), nameof(ContactInfo.ContactID))
                .TopN(1)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();

    private static void MapMember(CommunityMember member, ContactInfo contact)
    {
        contact.ContactLastName = string.IsNullOrWhiteSpace(member.LastName)
            ? member.UserName
            : member.LastName;

        if (!string.IsNullOrWhiteSpace(member.FirstName))
        {
            contact.ContactFirstName = member.FirstName;
        }
    }
}
