namespace Kentico.Community.Portal.Core.DataProtection;

/// <summary>
/// Keys of the Right to be forgotten dialog options (the admin dialog model's property names),
/// read by the portal's personal data eraser.
/// </summary>
public static class ErasureOptionKeys
{
    /// <summary>Archive the member, replace email and name, clear profile fields, avatar and external logins.</summary>
    public const string ANONYMIZE_MEMBER = "AnonymizeMember";
    /// <summary>Delete contacts with the email. Their activities and consent agreements go with them.</summary>
    public const string DELETE_CONTACTS = "DeleteContacts";
    /// <summary>Delete submissions of short-lived forms.</summary>
    public const string DELETE_SHORT_LIVED_FORM_DATA = "DeleteShortLivedFormData";
    /// <summary>Replace the email in submissions that are kept (history and unclassified forms).</summary>
    public const string SCRUB_FORM_EMAILS = "ScrubFormEmails";
    /// <summary>Remove recipient list memberships, and Q&amp;A discussion subscriptions and notification settings.</summary>
    public const string REMOVE_SUBSCRIPTIONS = "RemoveSubscriptions";
    /// <summary>Unlink email statistics hits from sent emails, then delete the sent email copies.</summary>
    public const string UNLINK_EMAIL_STATISTICS = "UnlinkEmailStatistics";

    public static IReadOnlyList<string> All { get; } =
    [
        ANONYMIZE_MEMBER,
        DELETE_CONTACTS,
        DELETE_SHORT_LIVED_FORM_DATA,
        SCRUB_FORM_EMAILS,
        REMOVE_SUBSCRIPTIONS,
        UNLINK_EMAIL_STATISTICS,
    ];

    /// <summary>
    /// Placeholder email for an erased data subject: unique per member so Identity still works, and
    /// non-routable (<c>.invalid</c> is reserved, RFC 2606). Xperience's member email validation
    /// rejects a bare <c>@invalid</c> domain.
    /// </summary>
    public static string ErasedEmail(int memberID) =>
        memberID > 0 ? $"erased-{memberID}@erased.invalid" : "erased@erased.invalid";
}
