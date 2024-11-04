using System.Collections.Immutable;
using Kentico.Community.Portal.Web.Components.ViewComponents.Navigation;
using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Navigation;

public class NotificationTrayViewComponent(IMediator mediator, AlertMessageCookieManager alertMessageCookieManager) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly AlertMessageCookieManager alertMessageCookieManager = alertMessageCookieManager;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var resp = await mediator.Send(new PortalWebsiteSettingsQuery());
        var settings = await mediator.Send(new WebsiteAlerSettingsQuery(resp.PortalSettings));

        var vm = GetModel(settings);

        return View("~/Components/ViewComponents/Navigation/NotificationTray.cshtml", vm);
    }

    private NotificationTrayViewModel GetModel(WebsiteAlertSettingsContent settings)
    {
        if (!settings.WebsiteAlertSettingsContentIsEnabled)
        {
            return new NotificationTrayViewModel([]);
        }

        var message = alertMessageCookieManager.GetVisibleAlerts(settings)
            .Select(c => new NotificationTrayMessage(c))
            .ToImmutableList();

        return new NotificationTrayViewModel(message);
    }
}

public record NotificationTrayViewModel(ImmutableList<NotificationTrayMessage> Messages);

public class NotificationTrayMessage
{
    public Guid GUID { get; }
    public Maybe<string> Title { get; }
    public HtmlString HTML { get; }
    public AlertMessageContentTypes Type { get; }

    public NotificationTrayMessage(AlertMessageContent message)
    {
        GUID = message.SystemFields.ContentItemGUID;
        Title = Maybe.From(message.AlertMessageContentTitle).MapNullOrWhiteSpaceAsNone();
        HTML = new HtmlString(message.AlertMessageContentMessageHTML);
        Type = message.AlertMessageContentTypeParsed;
    }
}
