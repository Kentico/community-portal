using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: EmailConfirmationWidget.IDENTIFIER,
    viewComponentType: typeof(EmailConfirmationWidget),
    name: "Email confirmation",
    propertiesType: typeof(EmailConfirmationWidgetProperties),
    Description = "Handles email confirmation requests with validation and messaging",
    IconClass = KenticoIcons.ADD_MODULE,
    AllowCache = false)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;

public class EmailConfirmationWidget(
    UserManager<CommunityMember> userManager,
    IPageBuilderContext pageBuilderContext) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.EmailConfirmationWidget";

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<EmailConfirmationWidgetProperties> _)
    {
        string memberEmail = HttpContext.Request.Query["memberEmail"].FirstOrDefault() ?? "";
        string confirmToken = HttpContext.Request.Query["confirmToken"].FirstOrDefault() ?? "";

        if (pageBuilderContext.Mode == ApplicationPageBuilderMode.Preview)
        {
            return View("~/Components/PageBuilder/Widgets/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel
            {
                State = EmailConfirmationState.Success_Confirmed,
                Message = "Your email has been confirmed."
            });
        }

        var member = await userManager.FindByEmailAsync(memberEmail);
        if (member is null)
        {
            return View("~/Components/PageBuilder/Widgets/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel()
            {
                State = EmailConfirmationState.Failure_ConfirmationFailed,
                Message = "We were unable to successfully confirm your email.",
                SendButtonText = "Resend confirmation email",
                Username = ""
            });
        }

        if (member.IsUnderModeration)
        {
            return View("~/Components/PageBuilder/Widgets/Registration/_ModerationStatus.cshtml");
        }

        if (member.Enabled)
        {
            return View("~/Components/PageBuilder/Widgets/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel
            {
                State = EmailConfirmationState.Success_AlreadyConfirmed,
                Message = "Your email is already verified."
            });
        }

        var confirmResult = await ConfirmEmailInternal(member, confirmToken);
        if (confirmResult.Succeeded)
        {
            return View("~/Components/PageBuilder/Widgets/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel
            {
                State = EmailConfirmationState.Success_Confirmed,
                Message = "Your email has been confirmed."
            });
        }

        return View("~/Components/PageBuilder/Widgets/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel()
        {
            State = EmailConfirmationState.Failure_ConfirmationFailed,
            Message = "We were unable to successfully confirm your email.",
            SendButtonText = "Resend confirmation email",
            Username = member.UserName!
        });
    }

    private async Task<IdentityResult> ConfirmEmailInternal(CommunityMember member, string confirmToken)
    {
        try
        {
            //Changes Enabled property of the user
            return await userManager.ConfirmEmailAsync(member, confirmToken);
        }
        catch (InvalidOperationException)
        {
            return IdentityResult.Failed(new IdentityError() { Description = "User not found." });
        }
    }
}

public class EmailConfirmationWidgetProperties : IWidgetProperties { }

public enum EmailConfirmationState
{
    Success_Confirmed,
    Success_AlreadyConfirmed,
    Failure_NotYetConfirmed,
    Failure_ConfirmationFailed
}

public class EmailConfirmationViewModel
{
    public EmailConfirmationState State { get; set; } = EmailConfirmationState.Failure_NotYetConfirmed;
    public string Message { get; set; } = "";
    public string SendButtonText { get; set; } = "";
    public string Username { get; set; } = "";
}
