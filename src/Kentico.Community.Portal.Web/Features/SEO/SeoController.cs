using System.Text;
using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sidio.Sitemap.AspNetCore;
using Sidio.Sitemap.Core;

namespace Kentico.Community.Portal.Web.Features.SEO;

[Route("")]
public class SeoController(SitemapRetriever retriever, LlmsTxtRetriever llmsTxtRetriever, IMediator mediator) : Controller
{
    /// <summary>
    /// Blockquote summary of llms.txt, written for the coding-agent persona (#340)
    /// </summary>
    public const string LLMS_TXT_SUMMARY = "Resources for developers working with Xperience by Kentico from the Kentico Community: Q&A discussions by topic, topic hubs, extensions and integrations, and blog posts on the product, its development, and digital marketing.";

    private readonly SitemapRetriever retriever = retriever;
    private readonly LlmsTxtRetriever llmsTxtRetriever = llmsTxtRetriever;
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

        string body = settings.GlobalContent.WebsiteGlobalContentRobotsTxt ?? "";

        // Absolute URL from the current request host and the sitemap route
        string? sitemapUrl = Url.Action(nameof(Sitemap), null, null, Request.Scheme, Request.Host.Value);

        return Content(AppendSitemapDirective(body, sitemapUrl), "text/plain", Encoding.UTF8);
    }

    /// <summary>
    /// Index of developer resources for coding agents (https://llmstxt.org)
    /// </summary>
    [HttpGet("llms.txt")]
    public async Task<ActionResult> LlmsTxt()
    {
        var settings = await mediator.Send(new PortalWebsiteSettingsQuery());
        var document = await llmsTxtRetriever.GetDocument();

        string siteName = settings.GlobalContent.WebsiteGlobalContentDisplayName;
        string baseURL = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        return Content(document.ToMarkdown(siteName, LLMS_TXT_SUMMARY, baseURL), "text/plain", Encoding.UTF8);
    }

    /// <summary>
    /// Appends a <c>Sitemap:</c> directive unless the editorial body already declares one.
    /// </summary>
    [NonAction]
    public static string AppendSitemapDirective(string body, string? sitemapUrl)
    {
        if (string.IsNullOrWhiteSpace(sitemapUrl) || HasSitemapDirective(body))
        {
            return body;
        }

        string trimmed = body.TrimEnd();

        // Blank line keeps the directive outside any User-agent group
        return trimmed.Length == 0
            ? $"Sitemap: {sitemapUrl}\n"
            : $"{trimmed}\n\nSitemap: {sitemapUrl}\n";
    }

    private static bool HasSitemapDirective(string body) =>
        body.Split('\n').Any(l => l.TrimStart().StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase));
}
