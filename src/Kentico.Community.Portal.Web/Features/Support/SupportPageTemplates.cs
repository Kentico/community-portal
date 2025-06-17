using Kentico.Community.Portal.Web.Features.Support;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.SupportPage_Default",
    name: "Support Page - Default",
    propertiesType: typeof(SupportPageTemplateProperties),
    customViewName: "~/Features/Support/SupportPage_Default.cshtml",
    ContentTypeNames = [SupportPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: SupportPage.CONTENT_TYPE_NAME,
    controllerType: typeof(SupportPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Support;

public class SupportPageTemplateProperties : IPageTemplateProperties { }

public class SupportPageTemplateController(
    IContentRetriever contentRetriever,
    WebPageMetaService metaService) : Controller
{
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly WebPageMetaService metaService = metaService;

    public async Task<ActionResult> Index()
    {
        var supportPage = await contentRetriever.RetrieveCurrentPage<SupportPage>();
        if (supportPage is null)
        {
            return NotFound();
        }

        metaService.SetMeta(supportPage);

        return new TemplateResult(supportPage);
    }
}
