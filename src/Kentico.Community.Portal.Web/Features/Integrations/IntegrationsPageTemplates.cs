using Kentico.Community.Portal.Web.Features.Integrations;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
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

        var landingPage = await mediator.Send(new IntegrationsLandingPageQuery(data.WebPage));

        metaService.SetMeta(new(landingPage));

        return new TemplateResult(landingPage);
    }
}
