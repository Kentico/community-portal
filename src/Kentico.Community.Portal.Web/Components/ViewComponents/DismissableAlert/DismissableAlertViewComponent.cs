using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.DismissableAlert;

public class DismissableAlertViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string message) =>
        View("~/Components/ViewComponents/DismissableAlert/DismissableAlert.cshtml", message);
}
