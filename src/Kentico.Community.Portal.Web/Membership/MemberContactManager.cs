using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;

namespace Kentico.Community.Portal.Web.Membership;

public class MemberContactManager(
    IInfoProvider<ContactInfo> contactProvider,
    ICurrentContactProvider currentContactProvider,
    IContactMergeService contactMerge,
    IContactCreator contactCreator,
    IReadOnlyModeProvider readOnlyProvider)
{
    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;
    private readonly ICurrentContactProvider currentContactProvider = currentContactProvider;
    private readonly IContactMergeService contactMerge = contactMerge;
    private readonly IContactCreator contactCreator = contactCreator;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    public async Task<ContactInfo?> SetMemberAsCurrentContact(CommunityMember member)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return null;
        }

        var contact = currentContactProvider.GetCurrentContact();

        if (contact is null)
        {
            return null;
        }

        contact.ContactLastName = string.IsNullOrWhiteSpace(member.LastName)
            ? member.UserName
            : member.LastName;

        if (!string.IsNullOrWhiteSpace(member.FirstName))
        {
            contact.ContactFirstName = member.FirstName;
        }

        if (!string.Equals(contact.ContactEmail, member.Email))
        {
            contact.ContactEmail = member.Email;
            contactMerge.MergeContactByEmail(contact);
            currentContactProvider.SetCurrentContact(contact);
        }

        await contactProvider.SetAsync(contact);

        return contact;
    }

    public ContactInfo ResetCurrentContact()
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return currentContactProvider.GetCurrentContact() ?? contactCreator.CreateAnonymousContact();
        }

        var newContact = contactCreator.CreateAnonymousContact();

        currentContactProvider.SetCurrentContact(newContact);

        return newContact;
    }
}
