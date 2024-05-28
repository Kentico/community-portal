using Kentico.Community.Portal.Web.Features.ResourceHub;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
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
    IMediator mediator,
    WebPageMetaService metaService,
    IWebPageDataContextRetriever contextRetriever) : Controller
{
    private readonly IMediator mediator = mediator;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IWebPageDataContextRetriever contextRetriever = contextRetriever;

    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var hubPage = await mediator.Send(new ResourceHubPageQuery(data.WebPage));

        metaService.SetMeta(new(hubPage));

        return new TemplateResult(hubPage);
    }
}
