using Kentico.Community.Portal.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.RecaptchaScript;

/// <summary>
/// Renders the Google reCAPTCHA script when the page asked for it (<c>RecaptchaScript</c>).
/// Place it after the page body.
/// </summary>
public class RecaptchaScriptViewComponent(IOptions<ReCaptchaSettings> options) : ViewComponent
{
    private readonly ReCaptchaSettings settings = options.Value;

    public IViewComponentResult Invoke()
    {
        if (!HttpContext.IsRecaptchaScriptRequired() || string.IsNullOrWhiteSpace(settings.SiteKey))
        {
            return Content("");
        }

        return View("~/Components/ViewComponents/RecaptchaScript/RecaptchaScript.cshtml", settings.SiteKey);
    }
}
