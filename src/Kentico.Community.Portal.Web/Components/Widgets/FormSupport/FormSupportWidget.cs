using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Kentico.Community.Portal.Web.Components.Widgets.FormSupport;
using Microsoft.Extensions.Options;
using Kentico.Community.Portal.Web.Infrastructure;

[assembly: RegisterWidget(
    identifier: FormSupportWidgetViewComponent.Identifier,
    viewComponentType: typeof(FormSupportWidgetViewComponent),
    name: "Form support",
    propertiesType: typeof(FormSupportWidgetProperties),
    Description = "Displays a support form.",
    IconClass = "icon-form")]

namespace Kentico.Community.Portal.Web.Components.Widgets.FormSupport;

/// <summary>
/// Controller for form support widget.
/// </summary>
public class FormSupportWidgetViewComponent : ViewComponent
{
    private readonly ReCaptchaSettings settings;

    /// <summary>
    /// Widget identifier.
    /// </summary>
    public const string Identifier = "CommunityPortal.FormSupport";

    /// <summary>
    /// Creates an instance of <see cref="FormSupportWidgetViewComponent"/> class.
    /// </summary>
    public FormSupportWidgetViewComponent(IOptions<ReCaptchaSettings> options) => settings = options.Value;

    public ViewViewComponentResult Invoke()
    {
        var model = new FormSupportWidgetViewModel()
        {
            SiteKey = settings.SiteKey
        };

        return View("~/Components/Widgets/FormSupport/FormSupportWidget.cshtml", model);
    }
}
