using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Support;

public class SupportPageHeaderViewComponent : ViewComponent
{
    private readonly IMediator mediator;
    private readonly WebPageMetaService metaService;

    public SupportPageHeaderViewComponent(IMediator mediator, WebPageMetaService metaService)
    {
        this.mediator = mediator;
        this.metaService = metaService;
    }

    public async Task<IViewComponentResult> InvokeAsync(RoutedWebPage page)
    {
        var supportPage = await mediator.Send(new SupportPageQuery(page));

        metaService.SetMeta(new(supportPage.Title, supportPage.ShortDescription));

        return View("~/Features/Support/Components/SupportPageHeader.cshtml", supportPage);
    }
}
