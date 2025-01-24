using System.Web;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Features.Registration;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.PasswordRecovery;

[Route("[controller]/[action]")]
public class PasswordRecoveryController(
    UserManager<CommunityMember> userManager,
    WebPageMetaService metaService,
    IMemberEmailService emailService) : Controller
{
    private const string VIEW_PATH_ERROR = "~/Features/PasswordRecovery/ResetPasswordError.cshtml";

    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IMemberEmailService emailService = emailService;

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

        var member = await userManager.FindByEmailAsync(model.Email);
        if (member is null)
        {
            return PartialView("~/Features/PasswordRecovery/_RequestRecoveryEmailForm.cshtml", new RequestRecoveryEmailViewModel());
        }
        if (member.IsUnderModeration())
        {
            return View("~/Features/Registration/_ModerationStatus.cshtml");
        }
        if (!member.Enabled)
        {
            return PartialView("~/Features/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel
            {
                State = EmailConfirmationState.Failure_NotYetConfirmed,
                Message = "You cannot reset your password until you confirm your email address.",
                SendButtonText = "Send confirmation email",
                Username = member.UserName!
            });
        }

        string token = await userManager.GeneratePasswordResetTokenAsync(member);
        string? resetURL = Url.Action(
            nameof(SetNewPassword),
            "PasswordRecovery",
            new { userId = member.Id, token = HttpUtility.UrlEncode(token) },
            Request.Scheme);

        if (resetURL is null)
        {
            ModelState.AddModelError("", "We were unable to generate the verification link to reset your password.");

            return PartialView("~/Features/PasswordRecovery/_RequestRecoveryEmailForm.cshtml", model);
        }

        await emailService
            .SendEmail(member, MemberEmailConfiguration.ResetPassword(resetURL));

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

        var member = await userManager.FindByIdAsync(userId?.ToString() ?? "");
        if (member is null)
        {
            ModelState.AddModelError(nameof(userId), "No matching user could be found.");

            return View(VIEW_PATH_ERROR);
        }
        if (member.IsUnderModeration())
        {
            return View("~/Features/Registration/_ModerationStatus.cshtml");
        }

        try
        {
            bool verificationResult = await userManager.VerifyUserTokenAsync(member,
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
            UserId = userId ?? 0,
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

        var member = await userManager.FindByIdAsync(model.UserId.ToString());
        if (member is null)
        {
            ModelState.AddModelError(nameof(model.UserId), "No matching user could be found.");

            return PartialView("~/Features/PasswordRecovery/_SetNewPasswordForm.cshtml", model);
        }
        if (member.IsUnderModeration())
        {
            return View("~/Features/Registration/_ModerationStatus.cshtml");
        }

        var result = await userManager.ResetPasswordAsync(member, HttpUtility.UrlDecode(model.Token), model.Password);

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
