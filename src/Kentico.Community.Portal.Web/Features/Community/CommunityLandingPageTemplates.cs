using CMS.Websites.Routing;
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
    ContentTypeNames = new[] { CommunityLandingPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: CommunityLandingPage.CONTENT_TYPE_NAME,
    controllerType: typeof(CommunityLandingPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Community;

public class CommunityLandingPageTemplateProperties : IPageTemplateProperties { }

public class CommunityLandingPageTemplateController : Controller
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;
    private readonly IWebsiteChannelContext channelContext;
    private readonly IWebPageDataContextRetriever contextRetriever;

    public CommunityLandingPageTemplateController(
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

        var page = await mediator.Send(new CommunityLandingPageQuery(data.WebPage, channelContext.WebsiteChannelName));

        metaService.SetMeta(new(page.CommunityLandingPageTitle, page.CommunityLandingPageShortDescription));

        var resp = await mediator.Send(new CommunityGroupContentsQuery());

        return new TemplateResult(new CommunityLandingPageViewModel(page, resp.Items));
    }
}

public class CommunityLandingPageViewModel
{
    public CommunityLandingPageViewModel(CommunityLandingPage page, IReadOnlyList<CommunityGroupContent> groups)
    {
        Page = page;
        Groups = groups;
    }

    public CommunityLandingPage Page { get; }
    public IReadOnlyList<CommunityGroupContent> Groups { get; }
}
