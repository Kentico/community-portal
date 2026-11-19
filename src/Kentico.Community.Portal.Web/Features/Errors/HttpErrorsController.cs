using Kentico.Community.Portal.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Errors;

[Route("error")]
public class HttpErrorsController(WebPageMetaService metaService) : Controller
{
    private readonly WebPageMetaService metaService = metaService;

    [HttpGet("{code:int}")]
    public ActionResult Error(int code)
    {
        /*
         * Re-executed requests (status code pages, exception handler) already carry an error status.
         * A direct request (ex: GET /error/404) would otherwise return 200 and look indexable.
         */
        if (Response.StatusCode < StatusCodes.Status400BadRequest)
        {
            code = code is >= StatusCodes.Status400BadRequest and <= 599 ? code : StatusCodes.Status404NotFound;
            Response.StatusCode = code;
        }

        if (code == 404)
        {
            SetMeta("Not Found", "The page you requested could not be found.");

            return View("~/Features/Errors/NotFound.cshtml", new ErrorPageViewModel { StatusCode = code });
        }

        if (code == 403)
        {
            SetMeta("Access Restricted", "You do not have permission to view this page.");

            return View("~/Features/Errors/Forbidden.cshtml", new ErrorPageViewModel { StatusCode = code });
        }

        SetMeta("Error", "There was a problem handling your request.");

        return View("~/Features/Errors/Exception.cshtml", new ErrorPageViewModel { StatusCode = code });
    }

    private void SetMeta(string title, string description) =>
        metaService.SetMeta(new WebPageMeta(title, description) { Robots = WebPageMeta.ROBOTS_NOINDEX });
}

public class ErrorPageViewModel
{
    public int StatusCode { get; set; }
}
