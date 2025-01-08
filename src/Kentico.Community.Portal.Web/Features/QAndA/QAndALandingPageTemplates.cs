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
    ContentTypeNames = [QAndALandingPage.CONTENT_TYPE_NAME],
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

public class QAndALandingPageTemplateController(IMediator mediator, WebPageMetaService metaService, IWebsiteChannelContext channelContext) : Controller
{
    private readonly IMediator mediator = mediator;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IWebsiteChannelContext channelContext = channelContext;

    public async Task<ActionResult> Index()
    {
        var landingPageResp = await mediator.Send(new QAndALandingPageQuery(channelContext.WebsiteChannelName));

        if (!landingPageResp.TryGetValue(out var landingPage))
        {
            return NotFound();
        }

        metaService.SetMeta(new(landingPage));

        return new TemplateResult(landingPage);
    }
}
