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
    ContentTypeNames = [LandingPage.CONTENT_TYPE_NAME],
    Description = "Default Landing Page template with a heading",
    IconClass = "xp-l-header-text"
)]

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.LandingPage_Empty",
    name: "Landing Page - Empty",
    propertiesType: typeof(LandingPageEmptyTemplateProperties),
    customViewName: "~/Features/LandingPages/LandingPage_Empty.cshtml",
    ContentTypeNames = [LandingPage.CONTENT_TYPE_NAME],
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

public class LandingPageTemplateController(
    IMediator mediator,
    WebPageMetaService metaService,
    IWebPageDataContextRetriever contextRetriever) : Controller
{
    private readonly IMediator mediator = mediator;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IWebPageDataContextRetriever contextRetriever = contextRetriever;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// This authorization filter must be applied at the Controller level because it interfers
    /// with the Page Builder if registered globally
    /// </remarks>
    [TypeFilter(typeof(ContentAuthorizationFilter))]
    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var landingPage = await mediator.Send(new LandingPageQuery(data.WebPage));

        metaService.SetMeta(new(landingPage)
        {
            CanonicalURL = landingPage.WebPageCanonicalURL
        });

        return new TemplateResult(landingPage);
    }
}
