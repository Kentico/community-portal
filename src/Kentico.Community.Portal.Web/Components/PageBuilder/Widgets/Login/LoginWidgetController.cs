using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CMS.Base;
using Htmx;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Login;

[Route("[controller]/[action]")]
public class LoginWidgetController(
    SignInManager<CommunityMember> signInManager,
    UserManager<CommunityMember> userManager,
    IStringLocalizer<SharedResources> localizer,
    ILogger<LoginWidgetController> logger,
    CookieConsentManager cookieConsentManager,
    IReadOnlyModeProvider readOnlyProvider) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(MemberRateLimitingConstants.Login)]
    public async Task<ActionResult> Login(LoginViewModel model)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return PartialView("~/Components/PageBuilder/Widgets/Login/_LoginForm.cshtml", model);
        }

        if (!ModelState.IsValid)
        {
            return PartialView("~/Components/PageBuilder/Widgets/Login/_LoginForm.cshtml", model);
        }

        var signInResult = SignInResult.Failed;

        try
        {
            var member = await GetMember();

            if (member is null)
            {
                signInResult = SignInResult.Failed;
            }
            else if (member.SignInState != MemberSignInState.Active)
            {
                /*
                 * Password first: only the right password learns why the account can't sign in.
                 * A wrong one gets the same response as an unknown account.
                 */
                if (!await userManager.CheckPasswordAsync(member, model.Password))
                {
                    signInResult = SignInResult.Failed;
                }
                else
                {
                    return member.SignInState switch
                    {
                        MemberSignInState.UnderModeration => PartialView("~/Components/PageBuilder/Widgets/Registration/_ModerationStatus.cshtml"),
                        MemberSignInState.EmailNotConfirmed => PartialView("~/Components/PageBuilder/Widgets/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel()
                        {
                            State = EmailConfirmationState.Failure_NotYetConfirmed,
                            Message = localizer["Your email has not been verified"],
                            SendButtonText = localizer["Send verification email"],
                            Username = member.UserName!
                        }),
                        // Disabled by an administrator. Confirming the email again won't help, so no verify prompt.
                        _ => PartialView("~/Components/PageBuilder/Widgets/Login/_AccountDisabled.cshtml"),
                    };
                }
            }
            else
            {
                signInResult = await signInManager.PasswordSignInAsync(member.UserName!, model.Password, model.StaySignedIn, false);
            }

            if (signInResult.Succeeded && member is not null)
            {
                await cookieConsentManager.OnMemberSignedIn(member);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(new EventId(0, "LOGIN_FAILURE"), ex, "Login failed for user input {UserIdentifier}", model.UserNameOrEmail);
            signInResult = SignInResult.Failed;
        }

        if (!signInResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, localizer["Your sign-in attempt was not successful. Please try again."].ToString());

            return PartialView("~/Components/PageBuilder/Widgets/Login/_LoginForm.cshtml", model);
        }

        // The hidden field is posted by the client: re-check it, never redirect off-site
        string returnURL = Url.IsLocalUrl(model.ReturnURL) ? model.ReturnURL : "/";

        Response.Htmx(h => h.Redirect(returnURL));
        return Ok();

        async Task<CommunityMember?> GetMember()
        {
            var member = await userManager.FindByNameAsync(model.UserNameOrEmail);
            if (member is not null)
            {
                return member;
            }

            return await userManager.FindByEmailAsync(model.UserNameOrEmail);
        }
    }
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your user name or email address")]
    [DisplayName("User name or email")]
    [MaxLength(100, ErrorMessage = "Maximum allowed length of the input text is {1}")]
    public string UserNameOrEmail { get; set; } = "";

    [DataType(DataType.Password)]
    [DisplayName("Password")]
    [MaxLength(100, ErrorMessage = "Maximum allowed length of the input text is {1}")]
    public string Password { get; set; } = "";

    [DisplayName("Stay signed in")]
    public bool StaySignedIn { get; set; } = false;

    [HiddenInput]
    public string ReturnURL { get; set; } = "";
}

[Route("[controller]/[action]")]
public class LogoutController(
    SignInManager<CommunityMember> signInManager,
    CookieConsentManager cookieConsentManager,
    IReadOnlyModeProvider readOnlyProvider) : Controller
{
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Logout()
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return StatusCode(503);
        }

        await signInManager.SignOutAsync();

        cookieConsentManager.ForgetVisitor();

        return Redirect("/");
    }
}
