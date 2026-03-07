using System.ComponentModel.DataAnnotations;
using CMS.Base;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.AccountRecovery;

[Route("[controller]/[action]")]
public class AccountRecoveryWidgetController(
    UserManager<CommunityMember> userManager,
    IMemberEmailService emailService,
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever webPageUrlRetriever,
    IReadOnlyModeProvider readOnlyProvider) : Controller
{
    /// <summary>
    /// Step 2
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(MemberRateLimitingConstants.ForgotPassword)]
    public async Task<ActionResult> RequestRecoveryEmail(AccountRecoveryFormViewModel model)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_AccountRecoveryForm.cshtml", model);
        }

        if (!ModelState.IsValid)
        {
            return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_AccountRecoveryForm.cshtml", model);
        }

        var member = await userManager.FindByEmailAsync(model.Email.Trim());
        if (member is null)
        {
            await Task.Delay(Random.Shared.Next(2000, 7000));
            return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_AccountRecoveryForm.cshtml", new AccountRecoveryFormViewModel());
        }
        if (member.IsUnderModeration)
        {
            return PartialView("~/Components/PageBuilder/Widgets/Registration/_ModerationStatus.cshtml");
        }
        if (!member.Enabled)
        {
            return PartialView("~/Components/PageBuilder/Widgets/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel
            {
                State = EmailConfirmationState.Failure_NotYetConfirmed,
                Message = "You cannot reset your password until you confirm your email address.",
                SendButtonText = "Send confirmation email",
                Username = member.UserName!
            });
        }

        string token = await userManager.GeneratePasswordResetTokenAsync(member);
        var pages = await contentRetriever.RetrieveContentByGuids<LandingPage>([SystemWebpages.ResetPassword.WebpageGUID]);
        if (pages.FirstOrDefault() is not LandingPage resetPasswordPage)
        {
            ModelState.AddModelError("", "We were unable to generate an account recovery email at this moment. Please contact support.");

            return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_AccountRecoveryForm.cshtml", model);
        }
        var webPageUrl = await webPageUrlRetriever.Retrieve(resetPasswordPage);
        string validationURL = QueryHelpers.AddQueryString(
            webPageUrl.AbsoluteUrl,
            new Dictionary<string, string?>
            {
                ["userId"] = member.Id.ToString(),
                ["token"] = token
            });

        if (string.IsNullOrWhiteSpace(validationURL))
        {
            ModelState.AddModelError("", "We were unable to generate an account recovery email at this moment. Please contact support.");

            return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_AccountRecoveryForm.cshtml", model);
        }

        await emailService.SendEmail(member, MemberEmailConfiguration.ResetPassword(validationURL));

        return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_AccountRecoveryForm.cshtml", new AccountRecoveryFormViewModel());
    }
}

public class AccountRecoveryFormViewModel
{
    [DataType(DataType.EmailAddress)]
    [Required(ErrorMessage = "The email address cannot be empty.")]
    [Display(Name = "Email address")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(254, ErrorMessage = "The Email address cannot be longer than 254 characters.")]
    public string Email { get; set; } = "";
}
