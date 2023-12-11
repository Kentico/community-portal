using CMS.Websites.Routing;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.QAndALandingPage_Default",
    name: "Q&A Landing Page - Default",
    propertiesType: typeof(QAndALandingPageTemplateProperties),
    customViewName: "~/Features/QAndA/QAndALandingPage_Default.cshtml",
    ContentTypeNames = new[] { QAndALandingPage.CONTENT_TYPE_NAME },
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: QAndALandingPage.CONTENT_TYPE_NAME,
    controllerType: typeof(QAndALandingPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndALandingPageTemplateProperties : IPageTemplateProperties
{
    [CheckBoxComponent(
        Label = "Display page description",
        ExplanationText = "If true, the Short Description of the page is displayed",
        Order = 1
    )]
    public bool DisplayPageDescription { get; set; } = true;
}

public class QAndALandingPageTemplateController : Controller
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;
    private readonly IWebsiteChannelContext channelContext;

    public QAndALandingPageTemplateController(IMediator mediator, WebPageMetaService metaService, IWebsiteChannelContext channelContext)
    {
        this.mediator = mediator;
        this.metaService = metaService;
        this.channelContext = channelContext;
    }

    public async Task<ActionResult> Index()
    {
        var landingPage = await mediator.Send(new QAndALandingPageQuery(channelContext.WebsiteChannelName));

        metaService.SetMeta(new(landingPage.Title, landingPage.QAndALandingPageShortDescription));

        return new TemplateResult(landingPage);
    }
}
