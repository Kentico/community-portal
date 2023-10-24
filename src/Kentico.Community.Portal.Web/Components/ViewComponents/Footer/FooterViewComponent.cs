using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Footer;

public class FooterViewComponent : ViewComponent
{
    private readonly IMediator mediator;

    public FooterViewComponent(IMediator mediator) => this.mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var resp = await mediator.Send(new WebsiteSettingsContentQuery());

        var content = string.IsNullOrWhiteSpace(resp.Settings.WebsiteSettingsContentFooterContentHTML)
            ? HtmlString.Empty
            : new HtmlString(resp.Settings.WebsiteSettingsContentFooterContentHTML);

        return View("~/Components/ViewComponents/Footer/Footer.cshtml", new FooterViewModel(content));
    }
}

public record FooterViewModel(HtmlString Content);
