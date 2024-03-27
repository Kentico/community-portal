using CMS.Helpers;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.AlertBox;

public class AlertBoxViewComponent(IMediator mediator, ICookieAccessor cookies) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly ICookieAccessor cookies = cookies;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = await mediator.Send(new WebsiteSettingsContentQuery());

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
