using System.Text;
using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sidio.Sitemap.AspNetCore;
using Sidio.Sitemap.Core;

namespace Kentico.Community.Portal.Web.Features.SEO;

[Route("")]
public class SeoController(SitemapRetriever retriever, IMediator mediator) : Controller
{
    private readonly SitemapRetriever retriever = retriever;
    private readonly IMediator mediator = mediator;


    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var nodes = await retriever.GetSitemapNodes();

        return new SitemapResult(new Sitemap(nodes));
    }

    [HttpGet("robots.txt")]
    public async Task<ActionResult> RobotsTxt()
    {
        var settings = await mediator.Send(new PortalWebsiteSettingsQuery());

        return Content(settings.GlobalContent.WebsiteGlobalContentRobotsTxt, "text/plain", Encoding.UTF8);
    }
}
