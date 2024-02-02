using CMS.Websites.Routing;
using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.BlogLandingPage_Default",
    name: "Blog Page - Default",
    propertiesType: typeof(BlogLandingPageTemplateProperties),
    customViewName: "~/Features/Blog/BlogLandingPage_Default.cshtml",
    ContentTypeNames = [BlogLandingPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: BlogLandingPage.CONTENT_TYPE_NAME,
    controllerType: typeof(BlogLandingPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Blog;

public class BlogLandingPageTemplateProperties : IPageTemplateProperties { }

public class BlogLandingPageTemplateController : Controller
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;
    private readonly IWebsiteChannelContext channelContext;
    private readonly IWebPageDataContextRetriever contextRetriever;

    public BlogLandingPageTemplateController(
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

        var landingPage = await mediator.Send(new BlogLandingPageQuery(data.WebPage, channelContext.WebsiteChannelName));

        metaService.SetMeta(new(landingPage.BlogLandingPageTitle, landingPage.BlogLandingPageShortDescription));

        return new TemplateResult(landingPage);
    }
}
