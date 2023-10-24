using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Community;

public class CommunityLandingPageViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;

    public CommunityLandingPageViewComponent(IMediator mediator, WebPageMetaService metaService)
    {
        this.mediator = mediator;
        this.metaService = metaService;
    }

    public async Task<IViewComponentResult> InvokeAsync(IRoutedWebPage page)
    {
        var resp = await mediator.Send(new CommunityLandingPageQuery(page));

        metaService.SetMeta(new(resp.Page.CommunityLandingPageTitle, resp.Page.CommunityLandingPageShortDescription));

        return View("~/Features/Community/Components/CommunityLandingPage.cshtml", resp.Page);
    }
}

