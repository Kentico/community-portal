using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.PageHeading;

public class PageHeadingViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IPortalPage page, bool displayDescription = true) =>
        View("~/Components/ViewComponents/PageHeading/PageHeading.cshtml", new PageHeadingViewModel(page.Title, displayDescription ? page.ShortDescription : null));
}

public record PageHeadingViewModel(string Title, string? Description);
