using CMS.Base;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;

[Route("[controller]/[action]")]
public class RegistrationWidgetController(
    UserManager<CommunityMember> userManager,
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever webPageUrlRetriever,
    CaptchaValidator captchaValidator,
    ILogger<RegistrationWidgetController> logger,
    IMemberEmailService emailService,
    ConsentManager consentManager,
    IReadOnlyModeProvider readOnlyProvider) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(MemberRateLimitingConstants.Registration)]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return PartialView("~/Components/PageBuilder/Widgets/Registration/_RegisterForm.cshtml", model);
        }

        var captchaResponse = await captchaValidator.ValidateCaptcha(model);

        if (!captchaResponse.IsSuccess)
        {
            ModelState.AddModelError("", "Captcha is invalid");
        }

        if (!ModelState.IsValid)
        {
            model.CaptchaToken = "";
            return PartialView("~/Components/PageBuilder/Widgets/Registration/_RegisterForm.cshtml", model);
        }

        var member = new CommunityMember
        {
            UserName = model.UserName,
            Email = model.Email,
            //We need to set Enabled to false because kentico uses enabled to set email confirmation (not .EmailConfirmed)
            Enabled = false,
            ModerationStatus = ModerationStatuses.None
        };

        var registerResult = IdentityResult.Failed();

        try
        {
            registerResult = await userManager.CreateAsync(member, model.Password);
        }
        catch (Exception ex)
        {
            logger.LogError(new EventId(0, "REGISTER_FAILURE"), ex, "Member registration failed for username {Username}", model.UserName);
            registerResult = IdentityResult.Failed([new() { Code = "Failure", Description = "Your registration was not successful." }]);
        }

        if (!registerResult.Succeeded)
        {
            foreach (string error in registerResult.Errors.Select(e => e.Description))
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return PartialView("~/Components/PageBuilder/Widgets/Registration/_RegisterForm.cshtml", model);
        }

        if (!await SendVerificationEmail(member))
        {
            ModelState.AddModelError("", "We were unable to generate a registration email. Please contact support.");

            return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_RegisterForm.cshtml", model);
        }

        if (model.ConsentAgreement)
        {
            await consentManager.AgreeToMarketingConsent();
        }

        return PartialView("~/Components/PageBuilder/Widgets/Registration/_RegisterForm.cshtml", model);
    }

    private async Task<bool> SendVerificationEmail(CommunityMember member)
    {
        string confirmToken = await userManager.GenerateEmailConfirmationTokenAsync(member);
        var pages = await contentRetriever.RetrieveContentByGuids<LandingPage>([SystemWebpages.EmailConfirmation.WebpageGUID]);
        if (pages.FirstOrDefault() is not LandingPage emailConfirmationPage)
        {
            return false;
        }

        var webPageUrl = await webPageUrlRetriever.Retrieve(emailConfirmationPage);
        string confirmationURL = QueryHelpers.AddQueryString(
            webPageUrl.AbsoluteUrl,
            new Dictionary<string, string?>
            {
                ["memberEmail"] = member.Email,
                ["confirmToken"] = confirmToken
            });

        await emailService.SendEmail(member, MemberEmailConfiguration.RegistrationConfirmation(confirmationURL));

        return true;
    }
}
