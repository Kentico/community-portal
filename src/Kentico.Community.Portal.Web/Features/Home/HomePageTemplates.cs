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
    ContentTypeNames = [HomePage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: HomePage.CONTENT_TYPE_NAME,
    controllerType: typeof(HomePageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Home;

public class HomePageTemplateProperties : IPageTemplateProperties { }

public class HomePageTemplateController(IMediator mediator, WebPageMetaService metaService, IWebPageDataContextRetriever contextRetriever) : Controller
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

        var homePage = await mediator.Send(new HomePageQuery(data.WebPage));

        metaService.SetMeta(new(homePage));

        return new TemplateResult(homePage);
    }
}
