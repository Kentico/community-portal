using CMS.DataProtection;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Web.Infrastructure;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Consents;

public class ConsentViewComponent(ConsentManager consentManager) : ViewComponent
{
    private readonly ConsentManager consentManager = consentManager;

    public async Task<IViewComponentResult> InvokeAsync(string consentName)
    {
        var consent = await consentManager.GetConsent(consentName);

        if (consent is null)
        {
            return View("~/Components/ViewComponents/Consents/Consent.cshtml", new ConsentViewModel());
        }

        var cultureText = await consent.GetConsentTextAsync(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        return View("~/Components/ViewComponents/Consents/Consent.cshtml", new ConsentViewModel
        {
            ConsentHTML = new(cultureText.ShortText)
        });
    }
}

public class ConsentViewModel
{
    public HtmlString ConsentHTML { get; set; } = HtmlString.Empty;
}
