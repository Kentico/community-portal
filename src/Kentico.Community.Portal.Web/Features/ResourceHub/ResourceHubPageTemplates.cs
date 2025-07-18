using Kentico.Community.Portal.Web.Features.ResourceHub;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.ResourceHubPage_Default",
    name: "ResourceHub Page - Default",
    propertiesType: typeof(ResourceHubPageTemplateProperties),
    customViewName: "~/Features/ResourceHub/ResourceHubPage_Default.cshtml",
    ContentTypeNames = [ResourceHubPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: ResourceHubPage.CONTENT_TYPE_NAME,
    controllerType: typeof(ResourceHubPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.ResourceHub;

public class ResourceHubPageTemplateProperties : IPageTemplateProperties { }

public class ResourceHubPageTemplateController(
    IContentRetriever contentRetriever,
    WebPageMetaService metaService) : Controller
{
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly WebPageMetaService metaService = metaService;

    public async Task<ActionResult> Index()
    {
        var hubPage = await contentRetriever.RetrieveCurrentPage<ResourceHubPage>();
        if (hubPage is null)
        {
            return NotFound();
        }

        metaService.SetMeta(hubPage);

        return new TemplateResult(hubPage);
    }
}
