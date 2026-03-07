using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: RegistrationWidget.IDENTIFIER,
    viewComponentType: typeof(RegistrationWidget),
    name: "Register",
    propertiesType: typeof(RegisterWidgetProperties),
    Description = "Displays a registration form for new users.",
    IconClass = KenticoIcons.ADD_MODULE,
    AllowCache = false)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;

public class RegistrationWidget : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.RegistrationWidget";

    public IViewComponentResult Invoke(ComponentViewModel<RegisterWidgetProperties> cvm)
    {
        var vm = new RegisterWidgetViewModel
        {
            RegisterModel = new RegisterViewModel()
        };

        return View("~/Components/PageBuilder/Widgets/Registration/Registration.cshtml", vm);
    }
}

public class RegisterWidgetProperties : BaseWidgetProperties
{
}

public class RegisterWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => "Register";
    public RegisterViewModel RegisterModel { get; set; } = new();
}
