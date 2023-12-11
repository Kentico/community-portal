using CMS.Websites.Routing;
using Kentico.Community.Portal.Web.Features.Support;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.SupportPage_Default",
    name: "Support Page - Default",
    propertiesType: typeof(SupportPageTemplateProperties),
    customViewName: "~/Features/Support/SupportPage_Default.cshtml",
    ContentTypeNames = new[] { SupportPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: SupportPage.CONTENT_TYPE_NAME,
    controllerType: typeof(SupportPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.Support;

public class SupportPageTemplateProperties : IPageTemplateProperties { }

public class SupportPageTemplateController : Controller
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;
    private readonly IWebsiteChannelContext channelContext;
    private readonly IWebPageDataContextRetriever contextRetriever;

    public SupportPageTemplateController(
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

        var supportPage = await mediator.Send(new SupportPageQuery(data.WebPage, channelContext.WebsiteChannelName));

        metaService.SetMeta(new(supportPage.Title, supportPage.ShortDescription));

        return new TemplateResult(supportPage);
    }
}
