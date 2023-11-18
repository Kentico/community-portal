using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Home;

public class HomePageHeadingViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;

    public HomePageHeadingViewComponent(IMediator mediator, WebPageMetaService metaService)
    {
        this.mediator = mediator;
        this.metaService = metaService;
    }

    public async Task<IViewComponentResult> InvokeAsync(RoutedWebPage page)
    {
        var homePage = await mediator.Send(new HomePageQuery(page));

        metaService.SetMeta(new("Home", homePage.HomePageShortDescription));

        return View("~/Features/Home/Components/HomePageHeading.cshtml", homePage);
    }
}
