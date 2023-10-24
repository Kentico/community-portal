using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.GTM;

public class GTMViewComponent : ViewComponent
{
    private readonly GoogleTagManagerSettings settings;

    public GTMViewComponent(IOptions<GoogleTagManagerSettings> options) => settings = options.Value;

    public IViewComponentResult Invoke(bool isHead) =>
        isHead
            ? View("~/Components/ViewComponents/GTM/GTMHead.cshtml", settings.Code)
            : View("~/Components/ViewComponents/GTM/GTMBody.cshtml", settings.Code);
}


public class GoogleTagManagerSettings
{
    public string Code { get; set; }
}
