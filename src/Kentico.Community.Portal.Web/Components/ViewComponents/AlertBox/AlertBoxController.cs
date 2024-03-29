﻿using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.AlertBox;

[Route("[controller]/[action]")]
public class AlertBoxController(IMediator mediator, ICookieAccessor cookies) : Controller
{
    public const string ROUTE_CONFIRM_ALERT = nameof(ROUTE_CONFIRM_ALERT);
    private readonly IMediator mediator = mediator;
    private readonly ICookieAccessor cookies = cookies;

    [HttpPost(Name = ROUTE_CONFIRM_ALERT)]
    public async Task<IActionResult> AlertBoxConfirm()
    {
        var resp = await mediator.Send(new WebsiteSettingsContentQuery());

        cookies.Set(CookieNames.ALERTBOX_CLOSED, "true", new()
        {
            Expires = DateTime.Now.AddDays(resp.WebsiteSettingsContentAlertBoxCookieExpirationDays),
            HttpOnly = false,
            Secure = true
        });

        return NoContent();
    }
}

public record AlertBoxCompleteViewModel(int AlertBoxCookieExpired);
