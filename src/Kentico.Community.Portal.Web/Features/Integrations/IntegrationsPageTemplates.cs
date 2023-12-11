using CMS.Websites.Routing;
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
    ContentTypeNames = new[] { IntegrationsLandingPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: IntegrationsLandingPage.CONTENT_TYPE_NAME,
    controllerType: typeof(IntegrationsLandingPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Integrations;

public class IntegrationsLandingPageTemplateProperties : IPageTemplateProperties { }

public class IntegrationsLandingPageTemplateController : Controller
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;
    private readonly IWebsiteChannelContext channelContext;
    private readonly IWebPageDataContextRetriever contextRetriever;

    public IntegrationsLandingPageTemplateController(
        IMediator mediator,
        WebPageMetaService metaService,
        IWebsiteChannelContext channelContext,
        IWebPageDataContextRetriever contextRetriever)
    {
        this.mediator = mediator;
        this.metaService = metaService;
        this.channelContext = channelContext;
        this.contextRetriever = contextRetriever;
    }

    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var landingPage = await mediator.Send(new IntegrationsLandingPageQuery(data.WebPage, channelContext.WebsiteChannelName));

        metaService.SetMeta(new(landingPage.IntegrationsLandingPageTitle, landingPage.IntegrationsLandingPageShortDescription));

        return new TemplateResult(landingPage);
    }
}
