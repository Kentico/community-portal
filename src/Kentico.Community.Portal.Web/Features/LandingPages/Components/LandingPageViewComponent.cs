using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.LandingPages;

public class LandingPageViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;

    public LandingPageViewComponent(IMediator mediator, WebPageMetaService metaService)
    {
        this.mediator = mediator;
        this.metaService = metaService;
    }

    public async Task<IViewComponentResult> InvokeAsync(RoutedWebPage page, LandingPageTemplateProperties props)
    {
        var resp = await mediator.Send(new LandingPageQuery(page));

        metaService.SetMeta(new(resp.Page.LandingPageTitle, resp.Page.LandingPageShortDescription));

        return props switch
        {
            LandingPageEmptyTemplateProperties => View("~/Features/LandingPages/Components/LandingPageEmpty.cshtml", resp.Page),
            LandingPageDefaultTemplateProperties or _ => View("~/Features/LandingPages/Components/LandingPage.cshtml", resp.Page)
        };
    }
}
