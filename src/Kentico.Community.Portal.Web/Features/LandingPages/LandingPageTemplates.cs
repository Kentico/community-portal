using CMS.Websites.Routing;
using Kentico.Community.Portal.Web.Features.LandingPages;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.LandingPage_Default",
    name: "Landing Page - Default",
    propertiesType: typeof(LandingPageDefaultTemplateProperties),
    customViewName: "~/Features/LandingPages/LandingPage_Default.cshtml",
    ContentTypeNames = new[] { LandingPage.CONTENT_TYPE_NAME },
    Description = "Default Landing Page template with a heading",
    IconClass = "xp-l-header-text"
)]

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.LandingPage_Empty",
    name: "Landing Page - Empty",
    propertiesType: typeof(LandingPageEmptyTemplateProperties),
    customViewName: "~/Features/LandingPages/LandingPage_Empty.cshtml",
    ContentTypeNames = new[] { LandingPage.CONTENT_TYPE_NAME },
    Description = "Landing Page template with no content and a single Editable Area",
    IconClass = "xp-l-text"
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: LandingPage.CONTENT_TYPE_NAME,
    controllerType: typeof(LandingPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.LandingPages;

public class LandingPageTemplateProperties : IPageTemplateProperties { }
public class LandingPageDefaultTemplateProperties : LandingPageTemplateProperties { }
public class LandingPageEmptyTemplateProperties : LandingPageTemplateProperties { }

public class LandingPageTemplateController : Controller
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;
    private readonly IWebsiteChannelContext channelContext;
    private readonly IWebPageDataContextRetriever contextRetriever;

    public LandingPageTemplateController(
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

        var landingPage = await mediator.Send(new LandingPageQuery(data.WebPage, channelContext.WebsiteChannelName));

        metaService.SetMeta(new(landingPage.LandingPageTitle, landingPage.LandingPageShortDescription));

        return new TemplateResult(landingPage);
    }
}
