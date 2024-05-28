using Kentico.Community.Portal.Web.Features.Community;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
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

        var page = await mediator.Send(new CommunityLandingPageQuery(data.WebPage));

        metaService.SetMeta(new(page));

        var resp = await mediator.Send(new CommunityGroupContentsQuery());

        return new TemplateResult(new CommunityLandingPageViewModel(page, resp.Items));
    }
}

public class CommunityLandingPageViewModel(CommunityLandingPage page, IReadOnlyList<CommunityGroupContent> groups)
{
    public CommunityLandingPage Page { get; } = page;
    public IReadOnlyList<CommunityGroupContent> Groups { get; } = groups;
}
