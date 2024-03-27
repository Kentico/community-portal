using CMS.Helpers;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

public class CookieBannerViewComponent(IMediator mediator, IHttpContextAccessor contextAccessor, ICookieAccessor cookies) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly ICookieAccessor cookies = cookies;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = await mediator.Send(new WebsiteSettingsContentQuery());

        bool accepted = ValidationHelper.GetBoolean(cookies.Get(CookieNames.COOKIE_ACCEPTANCE), false);
        bool isCookiePolicyPage = contextAccessor.HttpContext?.Request.Path.ToString().Equals("/cookies-policy", StringComparison.InvariantCultureIgnoreCase) ?? false;

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
    public string CookieBannerHeading { get; set; } = "";
    public HtmlString CookieBannerContentHTML { get; set; } = HtmlString.Empty;
    public string CookiePolicyPagePath { get; set; } = "";
    public bool HideBanner { get; set; }
}
