using CMS.Websites.Routing;
using Kentico.Community.Portal.Web.Features.Home;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.HomePage_Default",
    name: "Home Page - Default",
    propertiesType: typeof(HomePageTemplateProperties),
    customViewName: "~/Features/Home/HomePage_Default.cshtml",
    ContentTypeNames = new[] { HomePage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: HomePage.CONTENT_TYPE_NAME,
    controllerType: typeof(HomePageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Home;

public class HomePageTemplateProperties : IPageTemplateProperties { }

public class HomePageTemplateController : Controller
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;
    private readonly IWebsiteChannelContext channelContext;
    private readonly IWebPageDataContextRetriever contextRetriever;

    public HomePageTemplateController(IMediator mediator, WebPageMetaService metaService, IWebsiteChannelContext channelContext, IWebPageDataContextRetriever contextRetriever)
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

        var homePage = await mediator.Send(new HomePageQuery(data.WebPage, channelContext.WebsiteChannelName));

        metaService.SetMeta(new("Home", homePage.HomePageShortDescription));

        return new TemplateResult(homePage);
    }
}
