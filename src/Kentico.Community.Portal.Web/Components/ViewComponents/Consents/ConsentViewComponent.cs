using CMS.DataProtection;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Web.Infrastructure;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Consents;

public class ConsentViewComponent(ConsentRetriever consentRetriever) : ViewComponent
{
    private readonly ConsentRetriever consentRetriever = consentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(string consentName, bool fullText = false)
    {
        var consent = await consentRetriever.GetConsent(consentName);

        if (consent is null)
        {
            return View("~/Components/ViewComponents/Consents/Consent.cshtml", new ConsentViewModel());
        }

        var cultureText = await consent.GetConsentTextAsync(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        return View("~/Components/ViewComponents/Consents/Consent.cshtml", new ConsentViewModel
        {
            ConsentHTML = new(fullText ? cultureText.FullText : cultureText.ShortText)
        });
    }
}

public class ConsentViewModel
{
    public HtmlString ConsentHTML { get; set; } = HtmlString.Empty;
}
