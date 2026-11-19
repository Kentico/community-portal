using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.Membership;

namespace Kentico.Community.Portal.Web.Features.DataProtection;

/// <summary>
/// The identities the identity collector found for one email: the member (if any), stored contacts,
/// and every email address they use.
/// </summary>
public record DataSubject(IReadOnlyList<MemberInfo> Members, IReadOnlyList<ContactInfo> Contacts, IReadOnlyList<string> Emails)
{
    public IReadOnlyList<int> MemberIDs => [.. Members.Select(m => m.MemberID)];

    public IReadOnlyList<int> ContactIDs => [.. Contacts.Select(c => c.ContactID)];

    public bool IsEmpty => Members.Count == 0 && Contacts.Count == 0 && Emails.Count == 0;

    /// <remarks>
    /// Contacts with ID 0 are the identity collector's carrier for an email that has no stored
    /// contact (see <see cref="CommunityIdentityCollector"/>): their email counts, the contact doesn't.
    /// </remarks>
    public static DataSubject From(IEnumerable<BaseInfo> identities)
    {
        var list = identities.ToList();
        var members = list.OfType<MemberInfo>().Where(m => m.MemberID > 0).DistinctBy(m => m.MemberID).ToList();
        var allContacts = list.OfType<ContactInfo>().ToList();
        var contacts = allContacts.Where(c => c.ContactID > 0).DistinctBy(c => c.ContactID).ToList();

        var emails = allContacts.Select(c => c.ContactEmail)
            .Concat(members.Select(m => m.MemberEmail))
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DataSubject(members, contacts, emails);
    }
}
