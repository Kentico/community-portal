namespace Kentico.Community.Portal.Core.Membership;

/// <summary>
/// Code names of the recipient lists the portal code depends on.
/// </summary>
public static class RecipientListCodeNames
{
    /// <summary>
    /// Kentico Community news and updates. Subscriptions are tied to the
    /// <see cref="DataProtection.ConsentCodeNames.EMAIL_MARKETING"/> consent.
    /// </summary>
    public const string NEWSLETTER = "KenticoCommunityNewsletter";
}
