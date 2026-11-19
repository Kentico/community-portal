using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Community.Portal.Web.Membership;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Kentico.Xperience.ComponentIcons;

[assembly: RegisterWidget(
    identifier: EmailConfirmationWidget.IDENTIFIER,
    viewComponentType: typeof(EmailConfirmationWidget),
    name: "Email confirmation",
    propertiesType: typeof(EmailConfirmationWidgetProperties),
    Description = "Handles email confirmation requests with validation and messaging",
    IconClass = KenticoIcons.ADD_MODULE,
    AllowCache = false)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;

/// <summary>
/// Target of the registration confirmation link. Opening the link changes nothing: email scanners pre-open
/// links. The member clicks a button that POSTs to <see cref="EmailConfirmationWidgetController.ConfirmEmail"/>.
/// </summary>
public class EmailConfirmationWidget(
    RegistrationEmailConfirmationService confirmationService,
    IPageBuilderContext pageBuilderContext) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.EmailConfirmationWidget";
    public const string VIEW_PATH = "~/Components/PageBuilder/Widgets/Registration/EmailConfirmation.cshtml";

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<EmailConfirmationWidgetProperties> _)
    {
        string memberEmail = HttpContext.Request.Query["memberEmail"].FirstOrDefault() ?? "";
        string confirmToken = HttpContext.Request.Query["confirmToken"].FirstOrDefault() ?? "";

        if (pageBuilderContext.Mode == ApplicationPageBuilderMode.Preview)
        {
            return View(VIEW_PATH, EmailConfirmationViewModel.Confirmed());
        }

        var member = await confirmationService.FindMember(memberEmail);

        return member?.SignInState switch
        {
            MemberSignInState.UnderModeration => View("~/Components/PageBuilder/Widgets/Registration/_ModerationStatus.cshtml"),
            MemberSignInState.Active => View(VIEW_PATH, EmailConfirmationViewModel.AlreadyConfirmed()),
            MemberSignInState.EmailNotConfirmed when !string.IsNullOrWhiteSpace(confirmToken) => View(VIEW_PATH, new EmailConfirmationViewModel
            {
                State = EmailConfirmationState.Pending,
                Message = "Confirm your email to finish your registration.",
                SendButtonText = "Confirm my email",
                MemberEmail = memberEmail,
                ConfirmToken = confirmToken,
            }),
            _ => View(VIEW_PATH, EmailConfirmationViewModel.Failed(member)),
        };
    }
}

public class EmailConfirmationWidgetProperties : IWidgetProperties { }

public enum EmailConfirmationState
{
    /// <summary>The link was opened: the member still has to click the confirm button.</summary>
    Pending,
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
    /// <summary>Empty: no resend button.</summary>
    public string Username { get; set; } = "";
    /// <summary><see cref="EmailConfirmationState.Pending"/> only: posted back with the confirm button.</summary>
    public string MemberEmail { get; set; } = "";
    /// <inheritdoc cref="MemberEmail"/>
    public string ConfirmToken { get; set; } = "";

    public static EmailConfirmationViewModel Confirmed() => new()
    {
        State = EmailConfirmationState.Success_Confirmed,
        Message = "Your email has been confirmed."
    };

    public static EmailConfirmationViewModel AlreadyConfirmed() => new()
    {
        State = EmailConfirmationState.Success_AlreadyConfirmed,
        Message = "Your email is already verified."
    };

    /// <summary>Offers a new link only to a member who can still confirm.</summary>
    public static EmailConfirmationViewModel Failed(CommunityMember? member) => new()
    {
        State = EmailConfirmationState.Failure_ConfirmationFailed,
        Message = "We were unable to successfully confirm your email.",
        SendButtonText = "Resend confirmation email",
        Username = member is { CanConfirmEmail: true } ? member.UserName ?? "" : ""
    };
}
