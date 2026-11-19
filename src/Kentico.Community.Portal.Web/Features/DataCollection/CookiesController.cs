using CMS.Base;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CookiePreferences;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

[Route("[controller]/[action]")]
public class CookiesController(CookieConsentManager cookieConsentManager, IReadOnlyModeProvider readOnlyProvider) : Controller
{
    public const string ROUTE_SET_CONSENT = nameof(ROUTE_SET_CONSENT);

    private readonly CookieConsentManager cookieConsentManager = cookieConsentManager;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    /// <summary>
    /// Saves a choice from the banner or the cookie settings widget. In read-only mode no agreement
    /// can be recorded or revoked (see <see cref="CookieConsentManager.Apply"/>), and the settings
    /// widget's "Save choices" keeps the current tracking choice (its switch is disabled).
    /// </summary>
    [HttpPost(Name = ROUTE_SET_CONSENT)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Consent(CookieConsentRequest request)
    {
        var (analytics, tracking) = request.Choice switch
        {
            CookieConsentChoice.AcceptAll => (true, true),
            CookieConsentChoice.NecessaryOnly => (false, false),
            _ => (request.Analytics, request.Tracking),
        };

        if (!analytics)
        {
            cookieConsentManager.RemoveHostAnalyticsCookies();
        }

        // A disabled switch isn't posted, so Tracking would read as a withdrawal
        bool keepTracking = readOnlyProvider.IsReadOnly
            && request.Source == CookieConsentSource.Settings
            && request.Choice == CookieConsentChoice.Save;

        var choice = keepTracking
            ? await cookieConsentManager.ApplyAnalyticsOnly(analytics)
            : await cookieConsentManager.Apply(analytics, tracking);

        if (request.Source == CookieConsentSource.Settings)
        {
            return PartialView(
                "~/Components/PageBuilder/Widgets/CookiePreferences/_CookieSettingsResult.cshtml",
                new CookieSettingsViewModel(choice, User.Identity?.IsAuthenticated ?? false, ShowSavedMessage: true, readOnlyProvider.IsReadOnly));
        }

        return PartialView("~/Features/DataCollection/CookieBannerComplete.cshtml", new CookieBannerCompleteViewModel(choice));
    }
}

public enum CookieConsentChoice
{
    Save,
    AcceptAll,
    NecessaryOnly,
}

public enum CookieConsentSource
{
    Banner,
    Settings,
}

public class CookieConsentRequest
{
    public CookieConsentChoice Choice { get; set; }
    public CookieConsentSource Source { get; set; }
    public bool Analytics { get; set; }
    public bool Tracking { get; set; }
}

public record CookieBannerCompleteViewModel(ConsentCookie Choice);
