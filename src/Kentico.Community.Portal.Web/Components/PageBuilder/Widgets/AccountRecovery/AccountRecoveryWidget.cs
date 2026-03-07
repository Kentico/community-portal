using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.AccountRecovery;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: AccountRecoveryWidget.IDENTIFIER,
    viewComponentType: typeof(AccountRecoveryWidget),
    name: "Account Recovery",
    propertiesType: typeof(AccountRecoveryWidgetProperties),
    Description = "Displays a form to recover a member account.",
    IconClass = KenticoIcons.LOCK_UNLOCKED,
    AllowCache = false)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.AccountRecovery;

public class AccountRecoveryWidget : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.AccountRecoveryWidget";

    public IViewComponentResult Invoke(ComponentViewModel<AccountRecoveryWidgetProperties> _)
    {
        var vm = new AccountRecoveryWidgetViewModel
        {
            Form = new AccountRecoveryFormViewModel()
        };

        return View("~/Components/PageBuilder/Widgets/AccountRecovery/AccountRecovery.cshtml", vm);
    }
}

public class AccountRecoveryWidgetProperties : BaseWidgetProperties
{
}

public class AccountRecoveryWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => "Account Recovery";
    public AccountRecoveryFormViewModel Form { get; set; } = new();
}
