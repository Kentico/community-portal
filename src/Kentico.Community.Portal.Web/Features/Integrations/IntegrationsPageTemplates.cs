using Kentico.Community.Portal.Web.Features.Integrations;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.IntegrationsLandingPage_Default",
    name: "Integrations Landing Page - Default",
    propertiesType: typeof(IntegrationsLandingPageTemplateProperties),
    customViewName: "~/Features/Integrations/IntegrationsLandingPage_Default.cshtml",
    ContentTypeNames = [IntegrationsLandingPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: IntegrationsLandingPage.CONTENT_TYPE_NAME,
    controllerType: typeof(IntegrationsLandingPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Integrations;

public class IntegrationsLandingPageTemplateProperties : IPageTemplateProperties { }

public class IntegrationsLandingPageTemplateController(
    IContentRetriever contentRetriever,
    WebPageMetaService metaService) : Controller
{
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly WebPageMetaService metaService = metaService;

    public async Task<ActionResult> Index()
    {
        var landingPage = await contentRetriever.RetrieveCurrentPage<IntegrationsLandingPage>();
        if (landingPage is null)
        {
            return NotFound();
        }

        metaService.SetMeta(landingPage);

        return new TemplateResult(landingPage);
    }
}
