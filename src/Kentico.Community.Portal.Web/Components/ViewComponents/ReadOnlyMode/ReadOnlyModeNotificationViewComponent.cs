using CMS.Base;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.ReadOnlyMode;

/// <summary>
/// View component that displays a notification message when the application is in read-only mode.
/// </summary>
public class ReadOnlyModeNotificationViewComponent(IReadOnlyModeProvider readOnlyModeProvider) : ViewComponent
{
    private readonly IReadOnlyModeProvider readOnlyModeProvider = readOnlyModeProvider;

    public IViewComponentResult Invoke()
    {
        if (!readOnlyModeProvider.IsReadOnly)
        {
            return Content(string.Empty);
        }

        return View("~/Components/ViewComponents/ReadOnlyMode/ReadOnlyModeNotification.cshtml");
    }
}
