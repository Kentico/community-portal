using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.BlogLandingPage_Default",
    name: "Blog Page - Default",
    propertiesType: typeof(BlogLandingPageTemplateProperties),
    customViewName: "~/Features/Blog/BlogLandingPage_Default.cshtml",
    ContentTypeNames = [BlogLandingPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: BlogLandingPage.CONTENT_TYPE_NAME,
    controllerType: typeof(BlogLandingPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Blog;

public class BlogLandingPageTemplateProperties : IPageTemplateProperties { }

public class BlogLandingPageTemplateController(
    IContentRetriever contentRetriever,
    WebPageMetaService metaService) : Controller
{
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly WebPageMetaService metaService = metaService;

    public async Task<ActionResult> Index()
    {
        var landingPage = await contentRetriever.RetrieveCurrentPage<BlogLandingPage>();
        if (landingPage is null)
        {
            return NotFound();
        }

        metaService.SetMeta(landingPage);

        return new TemplateResult(landingPage);
    }
}
