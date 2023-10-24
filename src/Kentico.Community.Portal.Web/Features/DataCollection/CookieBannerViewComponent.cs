using CMS.Helpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Kentico.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

public class CookieBannerViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly IHttpContextAccessor contextAccessor;
    private readonly ICookieAccessor cookies;

    public CookieBannerViewComponent(IMediator mediator, IHttpContextAccessor contextAccessor, ICookieAccessor cookies)
    {
        this.mediator = mediator;
        this.contextAccessor = contextAccessor;
        this.cookies = cookies;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var resp = await mediator.Send(new WebsiteSettingsContentQuery());
        var settings = resp.Settings;

        bool accepted = ValidationHelper.GetBoolean(cookies.Get(CookieNames.COOKIE_ACCEPTANCE), false);
        bool isCookiePolicyPage = contextAccessor.HttpContext.Request.Path.ToString().Equals("/cookies-policy", StringComparison.InvariantCultureIgnoreCase);

        bool hideBanner = isCookiePolicyPage || accepted || string.Equals(cookies.Get(CookieNames.COOKIE_CONSENT_LEVEL), "4", StringComparison.OrdinalIgnoreCase);

        var vm = new CookieBannerViewModel()
        {
            CookieBannerHeading = settings.WebsiteSettingsContentCookieBannerHeading,
            CookieBannerContentHTML = new HtmlString(settings.WebsiteSettingsContentCookiebannerContentHTML),
            CookiePolicyPagePath = "/cookies-policy",
            HideBanner = hideBanner
        };

        return View("~/Features/DataCollection/CookieBanner.cshtml", vm);
    }
}

public class CookieBannerViewModel
{
    public string CookieBannerHeading { get; set; }
    public HtmlString CookieBannerContentHTML { get; set; }
    public string CookiePolicyPagePath { get; set; }
    public bool HideBanner { get; set; }
}
