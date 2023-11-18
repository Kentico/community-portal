using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Blog;

public class BlogLandingPageHeadingViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;

    public BlogLandingPageHeadingViewComponent(IMediator mediator, WebPageMetaService metaService)
    {
        this.mediator = mediator;
        this.metaService = metaService;
    }

    public async Task<IViewComponentResult> InvokeAsync(RoutedWebPage page)
    {
        var landingPage = await mediator.Send(new BlogLandingPageQuery(page));

        metaService.SetMeta(new(landingPage.BlogLandingPageTitle, landingPage.BlogLandingPageShortDescription));

        return View("~/Features/Blog/Components/BlogLandingPageHeading.cshtml", landingPage);
    }
}
