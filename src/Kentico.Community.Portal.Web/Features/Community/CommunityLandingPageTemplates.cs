using Kentico.Community.Portal.Web.Features.Community;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.CommunityLandingPage_Default",
    name: "Community Page - Default",
    propertiesType: typeof(CommunityLandingPageTemplateProperties),
    customViewName: "~/Features/Community/CommunityLandingPage_Default.cshtml",
    ContentTypeNames = [CommunityLandingPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: CommunityLandingPage.CONTENT_TYPE_NAME,
    controllerType: typeof(CommunityLandingPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Community;

public class CommunityLandingPageTemplateProperties : IPageTemplateProperties { }

public class CommunityLandingPageTemplateController(
    IContentRetriever contentRetriever,
    WebPageMetaService metaService) : Controller
{
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly WebPageMetaService metaService = metaService;

    public async Task<ActionResult> Index()
    {
        var page = await contentRetriever.RetrieveCurrentPage<CommunityLandingPage>();
        if (page is null)
        {
            return NotFound();
        }

        metaService.SetMeta(page);

        return new TemplateResult(new CommunityLandingPageViewModel(page));
    }
}

public record CommunityLandingPageViewModel(CommunityLandingPage Page);
