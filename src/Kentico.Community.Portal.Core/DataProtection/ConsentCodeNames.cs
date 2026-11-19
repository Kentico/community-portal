namespace Kentico.Community.Portal.Core.DataProtection;

/// <summary>
/// Code names of the Xperience consents the portal records agreements for.
/// </summary>
public static class ConsentCodeNames
{
    /// <summary>
    /// Website &amp; email tracking. Agreed when a visitor allows tracking; gates <c>CookieLevel.All</c>.
    /// </summary>
    public const string WEBSITE_TRACKING = "KenticoCommunityWebsiteTracking";

    /// <summary>
    /// Kentico Community news and updates (newsletter). Agreed when a newsletter subscription is
    /// confirmed, revoked on unsubscribe (see <see cref="EmailMarketingConsentManager"/>).
    /// </summary>
    public const string EMAIL_MARKETING = "EmailMarketing";
}
