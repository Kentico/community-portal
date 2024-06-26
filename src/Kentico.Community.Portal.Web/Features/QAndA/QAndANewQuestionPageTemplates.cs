using CMS.Websites.Routing;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
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

public class QAndANewQuestionPageTemplateController(IMediator mediator, WebPageMetaService metaService, IWebsiteChannelContext channelContext) : Controller
{
    private readonly IMediator mediator = mediator;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IWebsiteChannelContext channelContext = channelContext;

    public async Task<ActionResult> Index()
    {
        var questionPage = await mediator.Send(new QAndANewQuestionPageQuery(channelContext.WebsiteChannelName));

        metaService.SetMeta(new(questionPage));

        return new TemplateResult(questionPage);
    }
}
