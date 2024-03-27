using CMS.Helpers;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

public class CookiesInitViewComponent(ICookieAccessor cookies) : ViewComponent
{
    private readonly ICookieAccessor cookies = cookies;

    public IViewComponentResult Invoke()
    {
        int level = ValidationHelper.GetInteger(cookies.Get(CookieNames.COOKIE_CONSENT_LEVEL), 1);

        return View("~/Features/DataCollection/CookiesInit.cshtml", new CookiesInitViewModel(level));
    }
}

public record CookiesInitViewModel(int CookieConsentLevel);
