using CMS.Base;
using EnumsNET;

using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

[Route("[controller]/[action]")]
public class CookiesController(
    CookieConsentManager cookieConsentManager,
    ICookieAccessor cookies,
    TimeProvider clock,
    IReadOnlyModeProvider readOnlyProvider) : Controller
{
    public const string ROUTE_SET_PREFERENCES = nameof(ROUTE_SET_PREFERENCES);
    private readonly CookieConsentManager cookieConsentManager = cookieConsentManager;
    private readonly ICookieAccessor cookies = cookies;
    private readonly TimeProvider clock = clock;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    [HttpPost(Name = ROUTE_SET_PREFERENCES)]
    public async Task<IActionResult> CookiePreferences(CookieBannerCompleteViewModel requestModel)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return PartialView("~/Features/DataCollection/CookieBannerComplete.cshtml", requestModel);
        }

        if (!Enums.TryToObject<CookieConsentLevel>(requestModel.CookieLevelSelected, out var currentConsentValue, EnumValidation.IsDefined))
        {
            return NoContent();
        }

        await cookieConsentManager.SetCurrentCookieConsentLevel(currentConsentValue);

        // Set acceptance cookie
        cookies.Set(CookieNames.COOKIE_ACCEPTANCE, "true", new()
        {
            Expires = clock.GetLocalNow().DateTime.AddYears(1),
            HttpOnly = false,
            Secure = true
        });

        return PartialView("~/Features/DataCollection/CookieBannerComplete.cshtml", requestModel);
    }
}

public record CookieBannerCompleteViewModel(int CookieLevelSelected, bool ShowMessage, string MessageID = "");
