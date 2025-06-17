using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base.FormAnnotations;
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

public class QAndALandingPageTemplateController(IContentRetriever contentRetriever, WebPageMetaService metaService) : Controller
{
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly WebPageMetaService metaService = metaService;

    public async Task<ActionResult> Index()
    {
        var landingPage = await contentRetriever.RetrieveCurrentPage<QAndALandingPage>();
        if (landingPage is null)
        {
            return NotFound();
        }

        metaService.SetMeta(landingPage);

        return new TemplateResult(landingPage);
    }
}
