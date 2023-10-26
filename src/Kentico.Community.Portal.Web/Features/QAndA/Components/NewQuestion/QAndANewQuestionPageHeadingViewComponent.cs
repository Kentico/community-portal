using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndANewQuestionPageHeadingViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;

    public QAndANewQuestionPageHeadingViewComponent(IMediator mediator, WebPageMetaService metaService)
    {
        this.mediator = mediator;
        this.metaService = metaService;
    }

    public async Task<IViewComponentResult> InvokeAsync(IRoutedWebPage page)
    {
        var questionPage = await mediator.Send(new QAndANewQuestionPageQuery());

        metaService.SetMeta(new(questionPage.Title, questionPage.QAndANewQuestionPageShortDescription));

        return View("~/Features/QAndA/Components/NewQuestion/QAndANewQuestionPageHeading.cshtml", questionPage);
    }
}
