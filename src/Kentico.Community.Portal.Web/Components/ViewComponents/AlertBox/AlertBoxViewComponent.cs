using CMS.Helpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Kentico.Community.Portal.Web.Features.DataCollection;
using MediatR;
using Kentico.Web.Mvc;
using Kentico.Community.Portal.Web.Infrastructure;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.AlertBox;

public class AlertBoxViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly ICookieAccessor cookies;

    public AlertBoxViewComponent(IMediator mediator, ICookieAccessor cookies)
    {
        this.mediator = mediator;
        this.cookies = cookies;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var resp = await mediator.Send(new WebsiteSettingsContentQuery());

        var settings = resp.Settings;

        var vm = new AlertBoxViewModel
        {
            Closed = !settings.WebsiteSettingsContentIsAlertBoxEnabled
                || string.IsNullOrWhiteSpace(settings.WebsiteSettingsContentAlertBoxContentHTML)
                || ValidationHelper.GetBoolean(cookies.Get(CookieNames.ALERTBOX_CLOSED), false),

            AlertBoxMessageHTML = string.IsNullOrWhiteSpace(settings.WebsiteSettingsContentAlertBoxContentHTML)
                ? null
                : new HtmlString(settings.WebsiteSettingsContentAlertBoxContentHTML)
        };

        return View("~/Components/ViewComponents/AlertBox/AlertBox.cshtml", vm);
    }
}

public class AlertBoxViewModel
{
    public bool Closed { get; set; }
    public HtmlString? AlertBoxMessageHTML { get; set; }
}
