using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Helpers;
using Kentico.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

/// <summary>
/// Provides functionality for retrieving consents for contact.
/// </summary>
public class CookieConsentManager(
    IInfoProvider<ConsentInfo> consentInfoProvider,
    IConsentAgreementService consentAgreementService,
    ICurrentCookieLevelProvider currentCookieLevelProvider,
    ICookieAccessor cookies)
{
    private const string PREFERENCE_COOKIES_CONSENT_NAME = "CookiesPreference";
    private const string ANALYTICAL_COOKIES_CONSENT_NAME = "CookiesAnalytical";
    private const string MARKETING_COOKIES_CONSENT_NAME = "CookiesMarketing";

    private readonly IInfoProvider<ConsentInfo> consentInfoProvider = consentInfoProvider;
    private readonly IConsentAgreementService consentAgreementService = consentAgreementService;
    private readonly ICurrentCookieLevelProvider currentCookieLevelProvider = currentCookieLevelProvider;
    private readonly ICookieAccessor cookies = cookies;

    /// <summary>
    /// Sets current cookie consent level, internally sets system CookieLevel and agrees or revokes profiling consent.
    /// </summary>
    /// <param name="level">Cookie consent level to set</param>
    public void SetCurrentCookieConsentLevel(CookieConsentLevel level)
    {
        // Get current level and continue only if it has to be changed
        var originalLevel = Enum.TryParse<CookieConsentLevel>(cookies.Get(CookieNames.COOKIE_CONSENT_LEVEL), out var consent)
            ? consent
            : CookieConsentLevel.NotSet;

        if (originalLevel == level)
        {
            return;
        }

        // Get original contact before lowering the cookie level
        var originalContact = ContactManagementContext.GetCurrentContact();

        // Set cookie consent level into client's cookies
        cookies.Set(CookieNames.COOKIE_CONSENT_LEVEL, ((int)level).ToString(), new()
        {
            Expires = DateTime.Now.AddYears(1),
            HttpOnly = false,
            Secure = true
        });

        // Set system cookie level according consent level
        switch (level)
        {
            case CookieConsentLevel.NotSet:
                SetCookieLevelIfChanged(new(currentCookieLevelProvider.GetDefaultCookieLevel()));
                break;
            case CookieConsentLevel.Necessary:
                SetCookieLevelIfChanged(Kentico.Web.Mvc.CookieLevel.Essential);
                break;
            case CookieConsentLevel.Preference:
            case CookieConsentLevel.Analytical:
            case CookieConsentLevel.Marketing:
                SetCookieLevelIfChanged(Kentico.Web.Mvc.CookieLevel.Visitor);
                break;
            default:
                throw new NotSupportedException($"CookieConsentLevel {level} is not supported.");
        }

        // Get current contact after changing the level
        var currentContact = ContactManagementContext.GetCurrentContact();

        // Get consents
        var preferenceCookiesConsent = consentInfoProvider.Get(PREFERENCE_COOKIES_CONSENT_NAME);
        var analyticalCookiesConsent = consentInfoProvider.Get(ANALYTICAL_COOKIES_CONSENT_NAME);
        var marketingCookiesConsent = consentInfoProvider.Get(MARKETING_COOKIES_CONSENT_NAME);

        if (preferenceCookiesConsent == null || analyticalCookiesConsent == null || marketingCookiesConsent == null)
        {
            return;
        }

        // Agree cookie consent
        if (level == CookieConsentLevel.Preference && currentContact != null)
        {
            if (!consentAgreementService.IsAgreed(currentContact, preferenceCookiesConsent))
            {
                consentAgreementService.Agree(currentContact, preferenceCookiesConsent);
            }
        }
        else if (level == CookieConsentLevel.Analytical && currentContact != null)
        {
            if (!consentAgreementService.IsAgreed(currentContact, analyticalCookiesConsent))
            {
                consentAgreementService.Agree(currentContact, analyticalCookiesConsent);
            }
        }
        else if (level == CookieConsentLevel.Marketing && currentContact != null)
        {
            if (!consentAgreementService.IsAgreed(currentContact, marketingCookiesConsent))
            {
                consentAgreementService.Agree(currentContact, marketingCookiesConsent);
            }
        }

        // Revoke consents
        if (level != CookieConsentLevel.Preference && originalContact != null)
        {
            if (consentAgreementService.IsAgreed(originalContact, preferenceCookiesConsent))
            {
                consentAgreementService.Revoke(originalContact, preferenceCookiesConsent);
            }
        }

        if (level != CookieConsentLevel.Analytical && originalContact != null)
        {
            if (consentAgreementService.IsAgreed(originalContact, analyticalCookiesConsent))
            {
                consentAgreementService.Revoke(originalContact, analyticalCookiesConsent);
            }
        }

        if (level != CookieConsentLevel.Marketing && originalContact != null)
        {
            if (consentAgreementService.IsAgreed(originalContact, marketingCookiesConsent))
            {
                consentAgreementService.Revoke(originalContact, marketingCookiesConsent);
            }
        }
    }

    /// <summary>
    /// Sets CMSCookieLevel if it is different from the new one.
    /// </summary>
    /// <param name="newLevel"></param>
    private void SetCookieLevelIfChanged(Kentico.Web.Mvc.CookieLevel newLevel)
    {
        var currentCookieLevel = new Kentico.Web.Mvc.CookieLevel(currentCookieLevelProvider.GetCurrentCookieLevel());

        if (newLevel != currentCookieLevel)
        {
            currentCookieLevelProvider.SetCurrentCookieLevel(newLevel.Level);
        }
    }
}

public enum CookieConsentLevel
{
    /// <summary>
    /// Cookie consent level is not set.
    /// </summary>
    NotSet = 0,

    /// <summary>
    /// Only necessary cookies which are essential for running the system.
    /// </summary>
    Necessary = 1,

    /// <summary>
    /// Cookies for user preferences.
    /// </summary>
    Preference = 2,

    /// <summary>
    /// Cookies for site usage analysis.
    /// </summary>
    Analytical = 3,

    /// <summary>
    /// All cookies enabling to collect information about visitor.
    /// </summary>
    Marketing = 4
}

/// <summary>
/// Contains names of all custom cookies extending the solution. Each need to be registered in <see cref="CookieRegistrationModule"/>.
/// </summary>
public static class CookieNames
{
    // System cookies
    public const string COOKIE_CONSENT_LEVEL = "kenticocommunityportal.cookieconsentlevel";

    // Essential cookies
    public const string COOKIE_ACCEPTANCE = "kenticocommunityportal.cookielevelselection";
    public const string ALERTBOX_CLOSED = "kenticocommunityportal.alertboxclosed";
}
