using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Errors;

[Route("error")]
public class HttpErrorsController : Controller
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;

    public HttpErrorsController(IMediator mediator, WebPageMetaService metaService)
    {
        this.mediator = mediator;
        this.metaService = metaService;
    }

    [HttpGet("{code:int}")]
    public async Task<ActionResult> Error(int code)
    {
        if (code != 404)
        {
            metaService.SetMeta(new("Error", "There was a problem loading the page you requested."));

            return StatusCode(code);
        }

        var resp = await mediator.Send(new WebsiteSettingsContentQuery());
        var settings = resp.Settings;

        var model = new ErrorPageViewModel
        {
            MessageHTML = !string.IsNullOrWhiteSpace(settings.WebsiteSettingsContentNotFoundContentHTML)
                ? new(settings.WebsiteSettingsContentNotFoundContentHTML)
                : null
        };

        metaService.SetMeta(new("Not Found", "The page you requested could not be found."));

        return View("~/Features/Errors/NotFound.cshtml", model);
    }
}

public class ErrorPageViewModel
{
    public HtmlString? MessageHTML { get; set; }
}
