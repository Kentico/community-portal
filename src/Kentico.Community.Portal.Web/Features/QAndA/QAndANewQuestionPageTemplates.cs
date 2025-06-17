using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "KenticoCommunity.QAndANewQuestionPage_Default",
    name: "Q&A New Question Page - Default",
    propertiesType: typeof(QAndANewQuestionPageTemplateProperties),
    customViewName: "~/Features/QAndA/QAndANewQuestionPage_Default.cshtml",
    ContentTypeNames = [QAndANewQuestionPage.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: QAndANewQuestionPage.CONTENT_TYPE_NAME,
    controllerType: typeof(QAndANewQuestionPageTemplateController)
)]

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndANewQuestionPageTemplateProperties : IPageTemplateProperties { }

public class QAndANewQuestionPageTemplateController(IContentRetriever contentRetriever, WebPageMetaService metaService) : Controller
{
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly WebPageMetaService metaService = metaService;

    public async Task<ActionResult> Index()
    {
        var questionPage = await contentRetriever.RetrieveCurrentPage<QAndANewQuestionPage>();
        if (questionPage is null)
        {
            return NotFound();
        }

        metaService.SetMeta(questionPage);

        return new TemplateResult(questionPage);
    }
}
