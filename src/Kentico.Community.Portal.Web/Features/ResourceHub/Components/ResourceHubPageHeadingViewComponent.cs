using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.ResourceHub;

public class ResourceHubPageHeadingViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;

    public ResourceHubPageHeadingViewComponent(IMediator mediator, WebPageMetaService metaService)
    {
        this.mediator = mediator;
        this.metaService = metaService;
    }

    public async Task<IViewComponentResult> InvokeAsync(IRoutedWebPage page)
    {
        var hubPage = await mediator.Send(new ResourceHubPageQuery(page));

        metaService.SetMeta(new(hubPage.Title, hubPage.ShortDescription));

        return View("~/Features/ResourceHub/Components/ResourceHubPageHeading.cshtml", hubPage);
    }
}
