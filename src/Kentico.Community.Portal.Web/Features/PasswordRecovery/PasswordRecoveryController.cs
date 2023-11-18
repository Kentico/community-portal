using System.Web;
using CMS.EmailEngine;
using Kentico.Community.Portal.Web.Features.Registration;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Features.PasswordRecovery;

[Route("[controller]/[action]")]
public class PasswordRecoveryController : Controller
{
    /**
     * TODO: update View Location Expander to find this via conventions
     */
    private const string VIEW_PATH_ERROR = "~/Features/PasswordRecovery/ResetPasswordError.cshtml";

    private readonly IStringLocalizer<SharedResources> localizer;
    private readonly IEmailService emailService;
    private readonly SystemEmailOptions systemEmailOptions;
    private readonly UserManager<CommunityMember> userManager;
    private readonly SignInManager<CommunityMember> signInManager;
    private readonly WebPageMetaService metaService;

    public PasswordRecoveryController(
        UserManager<CommunityMember> userManager,
        SignInManager<CommunityMember> signInManager,
        IStringLocalizer<SharedResources> localizer,
        IOptions<SystemEmailOptions> systemEmailOptions,
        IEmailService emailService,
        WebPageMetaService metaService)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.localizer = localizer;
        this.emailService = emailService;
        this.systemEmailOptions = systemEmailOptions.Value;
        this.metaService = metaService;
    }

    /// <summary>
    /// Step 1
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public ActionResult RequestRecoveryEmail()
    {
        metaService.SetMeta(new("Reset Password", "Reset your password"));

        return View("~/Features/PasswordRecovery/RequestRecoveryEmail.cshtml", new RequestRecoveryEmailViewModel());
    }

    /// <summary>
    /// Step 2
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> RequestRecoveryEmail(RequestRecoveryEmailViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("~/Features/PasswordRecovery/_RequestRecoveryEmailForm.cshtml", model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            return PartialView("~/Features/PasswordRecovery/_RequestRecoveryEmailForm.cshtml", new RequestRecoveryEmailViewModel());
        }

        if (!user.Enabled)
        {
            return PartialView("~/Features/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel
            {
                State = EmailConfirmationState.Failure_NotYetConfirmed,
                Message = "You cannot reset your password until you confirm your email address.",
                SendButtonText = "Send confirmation email",
                Username = user.UserName
            });
        }

        string token = await userManager.GeneratePasswordResetTokenAsync(user);
        string? resetURL = Url.Action(
            nameof(SetNewPassword),
            "PasswordRecovery",
            new { userId = user.Id, token = HttpUtility.UrlEncode(token) },
            Request.Scheme);

        if (resetURL is null)
        {
            ModelState.AddModelError("", "We were unable to generate the verification link to reset your password.");

            return PartialView("~/Features/PasswordRecovery/_RequestRecoveryEmailForm.cshtml", model);
        }

        await emailService
            .SendEmail(new EmailMessage()
            {
                From = $"no-reply@{systemEmailOptions.SendingDomain}",
                Recipients = model.Email,
                Subject = "Password reset request",
                Body = $"""
                    <p>To reset your account's password, click <a href="{resetURL}">here</a>.</p>
                    <p style="margin-bottom: 1rem;">You can also copy and paste this URL into your browser.</p>
                    <p>{resetURL}</p>
                    """
            });

        return PartialView("~/Features/PasswordRecovery/_RequestRecoveryEmailForm.cshtml", new RequestRecoveryEmailViewModel());
    }

    /// <summary>
    /// Step 3
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult> SetNewPassword(int? userId, string? token)
    {
        metaService.SetMeta(new("Password Reset", "Reset your password."));

        if (string.IsNullOrEmpty(token))
        {
            ModelState.AddModelError(nameof(token), "URL is missing a verification token");

            return View(VIEW_PATH_ERROR);
        }

        if (userId is null)
        {
            ModelState.AddModelError(nameof(token), "URL is missing a user ID");

            return View(VIEW_PATH_ERROR);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            ModelState.AddModelError(nameof(userId), "No matching user could be found.");

            return View(VIEW_PATH_ERROR);
        }

        try
        {
            bool verificationResult = await userManager.VerifyUserTokenAsync(user,
                userManager.Options.Tokens.PasswordResetTokenProvider,
                UserManager<CommunityMember>.ResetPasswordTokenPurpose,
                HttpUtility.UrlDecode(token));

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

        var model = new SetNewPasswordViewModel
        {
            UserId = userId.Value,
            Token = token
        };

        return View("~/Features/PasswordRecovery/SetNewPassword.cshtml", model);
    }

    /// <summary>
    /// Step 4
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SetNewPassword(SetNewPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("~/Features/PasswordRecovery/_SetNewPasswordForm.cshtml", model);
        }

        var user = await userManager.FindByIdAsync(model.UserId.ToString());

        if (user is null)
        {
            ModelState.AddModelError(nameof(model.UserId), "No matching user could be found.");

            return PartialView("~/Features/PasswordRecovery/_SetNewPasswordForm.cshtml", model);
        }

        var result = await userManager.ResetPasswordAsync(user, HttpUtility.UrlDecode(model.Token), model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(nameof(model.Password), error.Description);
            }

            return PartialView("~/Features/PasswordRecovery/_SetNewPasswordForm.cshtml", model);
        }

        return PartialView("~/Features/PasswordRecovery/_SetNewPasswordForm.cshtml", new SetNewPasswordViewModel
        {
            SubmissionState = State.Reset
        });
    }
}
