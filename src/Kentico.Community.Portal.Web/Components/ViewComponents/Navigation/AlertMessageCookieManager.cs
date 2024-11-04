using CMS.ContactManagement;
using CMS.Core;
using Kentico.Web.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Navigation;

public class AlertMessageCookieManager(
    IConversionService conversionService,
    ICookieAccessor cookies,
    TimeProvider timeProvider,
    ICurrentContactProvider contactProvider)
{
    public const string COOKIE_NAME_PREFIX_NOTIFICATION_CLEARED = "kcp.notification-cleared.";

    private readonly IConversionService conversionService = conversionService;
    private readonly ICookieAccessor cookies = cookies;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ICurrentContactProvider contactProvider = contactProvider;

    public bool CanDisplay(AlertMessageContent message) =>
        !conversionService.GetBoolean(cookies.Get(GetCookieName(message)), false);

    public IEnumerable<AlertMessageContent> GetVisibleAlerts(WebsiteAlertSettingsContent settings)
    {
        var contact = contactProvider.GetCurrentContact();

        return settings
            .WebsiteAlertSettingsContentAlertMessageContents
            .Where(CanDisplay)
            .Where(m =>
            {
                if (m.AlertMessageContentDisplayForContactGroup == default)
                {
                    return true;
                }

                var contactGroups = contact?.ContactGroups is null
                    ? []
                    : contact.ContactGroups.ToList();

                return contactGroups.Any(c => c.ContactGroupGUID == m.AlertMessageContentDisplayForContactGroup.FirstOrDefault());
            });
    }


    public void SetClearedCookie(Guid messageGUID, WebsiteAlertSettingsContent settings)
    {
        string cookieName = GetCookieName(messageGUID);

        cookies.Set(cookieName, "true", new()
        {
            Expires = timeProvider.GetUtcNow().AddDays(settings.WebsiteAlertSettingsContentCookieExpirationDays),
            HttpOnly = false,
            Secure = true
        });
    }

    private static string GetCookieName(AlertMessageContent message) =>
        $"{COOKIE_NAME_PREFIX_NOTIFICATION_CLEARED}{message.SystemFields.ContentItemGUID}";
    private static string GetCookieName(Guid guid) =>
        $"{COOKIE_NAME_PREFIX_NOTIFICATION_CLEARED}{guid}";
}
