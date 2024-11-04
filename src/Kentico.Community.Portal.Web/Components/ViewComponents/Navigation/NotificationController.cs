using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Navigation;

[Route("[controller]/[action]")]
public class NotificationController(
    IMediator mediator,
    AlertMessageCookieManager alertMessageCookieManager) : Controller
{
    public const string ROUTE_CONFIRM_ALERT = nameof(ROUTE_CONFIRM_ALERT);
    private readonly IMediator mediator = mediator;
    private readonly AlertMessageCookieManager alertMessageCookieManager = alertMessageCookieManager;

    [HttpPost(Name = ROUTE_CONFIRM_ALERT)]
    public async Task<IActionResult> ClearNotification(Guid messageGUID)
    {
        var result = await mediator.Send(new PortalWebsiteSettingsQuery());
        var alertSettings = await mediator.Send(new WebsiteAlerSettingsQuery(result.PortalSettings));

        alertMessageCookieManager.SetClearedCookie(messageGUID, alertSettings);

        return Content("");
    }
}

public record AlertBoxCompleteViewModel(int AlertBoxCookieExpired);
