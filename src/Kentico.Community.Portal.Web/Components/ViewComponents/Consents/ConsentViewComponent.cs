using System.Globalization;
using CMS.DataProtection;
using Kentico.Community.Portal.Web.Infrastructure;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Consents;

public class ConsentViewComponent : ViewComponent
{
    private readonly ConsentManager consentManager;

    public ConsentViewComponent(ConsentManager consentManager) => this.consentManager = consentManager;

    public async Task<IViewComponentResult> InvokeAsync(string consentName)
    {
        var consent = await consentManager.GetConsent(consentName);

        if (consent is null)
        {
            return View("~/Components/ViewComponents/Consents/Consent.cshtml", new ConsentViewModel());
        }

        var cultureText = await consent.GetConsentTextAsync(CultureInfo.CurrentCulture.Name);

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
