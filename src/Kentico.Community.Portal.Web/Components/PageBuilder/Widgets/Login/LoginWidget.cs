using System.Web;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Login;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: LoginWidget.IDENTIFIER,
    viewComponentType: typeof(LoginWidget),
    name: "Login",
    propertiesType: typeof(LoginWidgetProperties),
    Description = "Displays a login form for user authentication.",
    IconClass = KenticoIcons.KEY,
    AllowCache = false)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Login;

public class LoginWidget : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.LoginWidget";

    public IViewComponentResult Invoke(ComponentViewModel<LoginWidgetProperties> _)
    {
        string returnURL = HttpContext.Request.Query["ReturnUrl"]
            .Select(HttpUtility.UrlDecode)
            .WhereNotNull()
            .Where(v =>
                !v.Contains("login")
                && !v.Contains("register"))
            .Select(v => Url.IsLocalUrl(v) ? v : "/")
            .FirstOrDefault() ?? "/";

        var vm = new LoginWidgetViewModel
        {
            LoginModel = new LoginViewModel
            {
                ReturnURL = returnURL
            }
        };

        return View("~/Components/PageBuilder/Widgets/Login/Login.cshtml", vm);
    }
}

public class LoginWidgetProperties : BaseWidgetProperties
{
}

public class LoginWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => "Login";
    public LoginViewModel LoginModel { get; set; } = new();
}
