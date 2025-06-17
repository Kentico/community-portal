using Kentico.Community.Portal.Web.Features.Home;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
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

public class HomePageTemplateController(IContentRetriever contentRetriever, WebPageMetaService metaService) : Controller
{
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly WebPageMetaService metaService = metaService;

    public async Task<ActionResult> Index()
    {
        var homePage = await contentRetriever.RetrieveCurrentPage<HomePage>();
        if (homePage is null)
        {
            return NotFound();
        }

        metaService.SetMeta(homePage);

        return new TemplateResult(homePage);
    }
}
