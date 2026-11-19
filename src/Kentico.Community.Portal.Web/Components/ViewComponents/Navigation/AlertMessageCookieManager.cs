using CMS.ContactManagement;
using Kentico.Web.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Navigation;

/// <summary>
/// Remembers dismissed site alerts in one cookie holding their GUIDs. One cookie, because
/// <see cref="CookieLevelOptions"/> registers exact names only (no prefixes) and the cookie must be
/// registered at <see cref="CookieLevel.Essential"/> to work without tracking consent.
/// </summary>
public class AlertMessageCookieManager(
    ICookieAccessor cookies,
    TimeProvider timeProvider)
{
    public const string COOKIE_NAME_NOTIFICATION_CLEARED = "kcp.notification-cleared";

    /// <summary>
    /// Keeps the cookie below browser size limits. The oldest dismissals drop off first.
    /// </summary>
    private const int MAX_CLEARED_ALERTS = 50;

    private readonly ICookieAccessor cookies = cookies;
    private readonly TimeProvider timeProvider = timeProvider;

    public bool CanDisplay(AlertMessageContent message) =>
        !GetClearedAlerts().Contains(message.SystemFields.ContentItemGUID);

    public IEnumerable<AlertMessageContent> GetVisibleAlerts(WebsiteAlertSettingsContent settings)
    {
        // Contact group targeting needs a tracked contact. Without one, targeted alerts don't show.
        var contact = ContactManagementContext.GetCurrentContact(createAnonymous: false);
        var cleared = GetClearedAlerts();

        return settings
            .WebsiteAlertSettingsContentAlertMessageContents
            .Where(m => !cleared.Contains(m.SystemFields.ContentItemGUID))
            .Where(m =>
            {
                // No contact group selected (null or empty): shown to everyone
                if (m.AlertMessageContentDisplayForContactGroup?.FirstOrDefault() is not Guid groupGuid
                    || groupGuid == Guid.Empty)
                {
                    return true;
                }

                return contact?.ContactGroups?.Any(c => c.ContactGroupGUID == groupGuid) ?? false;
            });
    }

    public void SetClearedCookie(Guid messageGUID, WebsiteAlertSettingsContent settings)
    {
        var cleared = GetClearedAlerts().Where(g => g != messageGUID).ToList();
        cleared.Add(messageGUID);

        cookies.Set(COOKIE_NAME_NOTIFICATION_CLEARED, string.Join('|', cleared.TakeLast(MAX_CLEARED_ALERTS)), new()
        {
            Expires = timeProvider.GetUtcNow().AddDays(settings.WebsiteAlertSettingsContentCookieExpirationDays),
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
        });
    }

    private List<Guid> GetClearedAlerts() =>
        (cookies.Get(COOKIE_NAME_NOTIFICATION_CLEARED) ?? "")
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s, out var guid) ? guid : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();
}
