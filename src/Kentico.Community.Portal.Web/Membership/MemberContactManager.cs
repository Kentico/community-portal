using CMS.ContactManagement;

namespace Kentico.Community.Portal.Web.Membership;

public class MemberContactManager
{
    private readonly IContactInfoProvider contactProvider;
    private readonly ICurrentContactProvider currentContactProvider;
    private readonly IContactMergeService contactMerge;
    private readonly IContactCreator contactCreator;

    public MemberContactManager(
        IContactInfoProvider contactProvider,
        ICurrentContactProvider currentContactProvider,
        IContactMergeService contactMerge,
        IContactCreator contactCreator
    )
    {
        this.contactProvider = contactProvider;
        this.currentContactProvider = currentContactProvider;
        this.contactMerge = contactMerge;
        this.contactCreator = contactCreator;
    }

    public ContactInfo? SetMemberAsCurrentContact(CommunityMember member)
    {
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

        contactProvider.Set(contact);

        return contact;
    }

    public ContactInfo ResetCurrentContact()
    {
        var newContact = contactCreator.CreateAnonymousContact();

        currentContactProvider.SetCurrentContact(newContact);

        return newContact;
    }
}
