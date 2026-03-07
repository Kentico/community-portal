using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.AccountRecovery;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: ResetPasswordWidget.IDENTIFIER,
    viewComponentType: typeof(ResetPasswordWidget),
    name: "Reset Password",
    propertiesType: typeof(ResetPasswordWidgetProperties),
    Description = "Displays a form to reset a member's password.",
    IconClass = KenticoIcons.LOCK_UNLOCKED,
    AllowCache = false)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.AccountRecovery;

public class ResetPasswordWidget(
    UserManager<CommunityMember> userManager,
    IPageBuilderContext pageBuilderContext) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.ResetPasswordWidget";

    private const string VIEW_PATH_ERROR = "~/Components/PageBuilder/Widgets/AccountRecovery/AccountRecoveryError.cshtml";

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<ResetPasswordWidgetProperties> _)
    {
        if (pageBuilderContext.Mode != ApplicationPageBuilderMode.Live)
        {
            var vm = new ResetPasswordWidgetViewModel
            {
                Form = new ResetPasswordFormViewModel()
            };

            return View("~/Components/PageBuilder/Widgets/AccountRecovery/ResetPassword.cshtml", vm);
        }

        var userId = HttpContext.Request.Query["userId"]
            .TryFirst()
            .Bind(v => int.TryParse(v, out int userId) ? userId : Maybe<int>.None);
        var verificationToken = HttpContext.Request.Query["token"]
            .TryFirst()
            .Bind(v => string.IsNullOrWhiteSpace(v) ? Maybe<string>.None : v);

        if (!verificationToken.TryGetValue(out string? token))
        {
            ModelState.AddModelError(nameof(token), "The verification link is missing a token.");

            return View(VIEW_PATH_ERROR);
        }

        if (!userId.TryGetValue(out int id))
        {
            ModelState.AddModelError(nameof(userId), "The verification link invalid.");

            return View(VIEW_PATH_ERROR);
        }

        var member = await userManager.FindByIdAsync(id.ToString());
        if (member is null)
        {
            ModelState.AddModelError(nameof(userId), "The verification link is invalid.");

            return View(VIEW_PATH_ERROR);
        }
        if (member.IsUnderModeration)
        {
            return View("~/Components/PageBuilder/Widgets/Registration/_ModerationStatus.cshtml");
        }

        try
        {
            bool verificationResult = await userManager.VerifyUserTokenAsync(member,
                userManager.Options.Tokens.PasswordResetTokenProvider,
                UserManager<CommunityMember>.ResetPasswordTokenPurpose,
                token);

            if (!verificationResult)
            {
                ModelState.AddModelError(nameof(token), "Could not validate the verification token.");

                return View(VIEW_PATH_ERROR);
            }
        }
        catch (InvalidOperationException)
        {
            return View(VIEW_PATH_ERROR);
        }

        var form = new ResetPasswordFormViewModel
        {
            UserId = id,
            Token = token
        };

        return View("~/Components/PageBuilder/Widgets/AccountRecovery/ResetPassword.cshtml", new ResetPasswordWidgetViewModel { Form = form });
    }
}

public class ResetPasswordWidgetProperties : BaseWidgetProperties
{
}

public class ResetPasswordWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => "Account Recovery";
    public ResetPasswordFormViewModel Form { get; set; } = new();
}
