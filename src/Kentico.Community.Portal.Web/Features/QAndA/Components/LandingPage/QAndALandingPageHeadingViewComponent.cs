using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndALandingPageHeadingViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;

    public QAndALandingPageHeadingViewComponent(IMediator mediator, WebPageMetaService metaService)
    {
        this.mediator = mediator;
        this.metaService = metaService;
    }

    public async Task<IViewComponentResult> InvokeAsync(QAndALandingPageTemplateProperties props)
    {
        var landingPage = await mediator.Send(new QAndALandingPageQuery());

        metaService.SetMeta(new(landingPage.DocumentName, landingPage.QAndALandingPageShortDescription));

        return View("~/Features/QAndA/Components/LandingPage/QAndALandingPageHeading.cshtml", new QAndALandingPageHeadingViewModel
        {
            Page = landingPage,
            Props = props
        });
    }
}

public class QAndALandingPageHeadingViewModel
{
    public QAndALandingPage Page { get; set; }
    public QAndALandingPageTemplateProperties Props { get; set; }
}
