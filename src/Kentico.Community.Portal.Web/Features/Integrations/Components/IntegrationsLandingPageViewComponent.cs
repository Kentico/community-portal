using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Integrations;

public class IntegrationsLandingPageViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;

    public IntegrationsLandingPageViewComponent(IMediator mediator, WebPageMetaService metaService)
    {
        this.mediator = mediator;
        this.metaService = metaService;
    }

    public async Task<IViewComponentResult> InvokeAsync(RoutedWebPage page)
    {
        var resp = await mediator.Send(new IntegrationsLandingPageQuery(page));

        metaService.SetMeta(new(resp.Page.IntegrationsLandingPageTitle, resp.Page.IntegrationsLandingPageShortDescription));

        return View("~/Features/Integrations/Components/IntegrationsLandingPage.cshtml", resp.Page);
    }
}
