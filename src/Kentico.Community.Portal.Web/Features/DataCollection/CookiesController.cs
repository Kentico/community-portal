using EnumsNET;
using Kentico.Community.Portal.Core;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

[Route("[controller]/[action]")]
public class CookiesController : Controller
{
    public const string ROUTE_SET_PREFERENCES = nameof(ROUTE_SET_PREFERENCES);
    private readonly CookieConsentManager cookieConsentManager;
    private readonly ICookieAccessor cookies;
    private readonly ISystemClock clock;

    public CookiesController(
        CookieConsentManager cookieConsentManager,
        ICookieAccessor cookies,
        ISystemClock clock)
    {
        this.cookieConsentManager = cookieConsentManager;
        this.cookies = cookies;
        this.clock = clock;
    }

    [HttpPost(Name = ROUTE_SET_PREFERENCES)]
    public IActionResult CookiePreferences(CookieBannerCompleteViewModel requestModel)
    {
        if (!Enums.TryToObject<CookieConsentLevel>(requestModel.CookieLevelSelected, out var currentConsentValue, EnumValidation.IsDefined))
        {
            return NoContent();
        }

        cookieConsentManager.SetCurrentCookieConsentLevel(currentConsentValue);

        // Set acceptance cookie
        cookies.Set(CookieNames.COOKIE_ACCEPTANCE, "true", new()
        {
            Expires = clock.Now.AddYears(1),
            HttpOnly = false,
            Secure = true
        });

        return PartialView("~/Features/DataCollection/CookieBannerComplete.cshtml", requestModel);
    }
}

public record CookieBannerCompleteViewModel(int CookieLevelSelected, bool ShowMessage, string MessageID = "");
