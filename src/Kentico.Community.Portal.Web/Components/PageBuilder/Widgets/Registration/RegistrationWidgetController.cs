using System.Security.Cryptography;
using System.Text.Json;
using CMS.Base;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Web.Components.TagHelpers;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Features.Members.EmailVerification;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;

[Route("[controller]/[action]")]
public class RegistrationWidgetController(
    UserManager<CommunityMember> userManager,
    CaptchaValidator captchaValidator,
    ILogger<RegistrationWidgetController> logger,
    IReadOnlyModeProvider readOnlyProvider,
    IOptions<TermsOptions> termsOptions,
    IWidgetPropertiesSerializer propertiesSerializer,
    MarkdownRenderer markdownRenderer,
    MemberEmailVerificationService emailVerificationService,
    RegistrationEmailConfirmationService confirmationService) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(MemberRateLimitingConstants.Registration)]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // The re-rendered form shows the widget's text, not the defaults
        model.Content = new RegisterFormContent(GetWidgetProperties(), markdownRenderer);

        if (readOnlyProvider.IsReadOnly)
        {
            return PartialView("~/Components/PageBuilder/Widgets/Registration/_RegisterForm.cshtml", model);
        }

        var captchaResponse = await captchaValidator.ValidateCaptcha(model, "Registration");

        if (!captchaResponse.IsSuccess)
        {
            ModelState.AddModelError("", "Captcha is invalid");
        }

        // Also rejects an unticked Terms of Use box (RegisterViewModel.AcceptTerms)
        if (!ModelState.IsValid)
        {
            model.CaptchaToken = "";
            return PartialView("~/Components/PageBuilder/Widgets/Registration/_RegisterForm.cshtml", model);
        }

        // Also covers another member's verified alternate email, which Identity's unique email check doesn't.
        // Same message as IdentityErrorDescriber.DuplicateEmail, so this reveals nothing new.
        if (await emailVerificationService.IsEmailInUse(model.Email, 0))
        {
            ModelState.AddModelError(string.Empty, new IdentityErrorDescriber().DuplicateEmail(model.Email).Description);
            model.CaptchaToken = "";
            return PartialView("~/Components/PageBuilder/Widgets/Registration/_RegisterForm.cshtml", model);
        }

        var now = DateTime.Now;
        var member = new CommunityMember
        {
            UserName = model.UserName,
            Email = model.Email,
            //We need to set Enabled to false because kentico uses enabled to set email confirmation (not .EmailConfirmed)
            Enabled = false,
            ModerationStatus = ModerationStatuses.None,
            TermsAcceptedVersion = termsOptions.Value.CurrentVersion,
            TermsAcceptedDate = now,
            // Subscribed on email confirmation (EmailConfirmationWidget), not now
            NewsletterOptInRequestedDate = model.SubscribeToNewsletter ? now : null,
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

        if (!await confirmationService.SendConfirmationEmail(member))
        {
            ModelState.AddModelError("", "We were unable to generate a registration email. Please contact support.");

            return PartialView("~/Components/PageBuilder/Widgets/Registration/_RegisterForm.cshtml", model);
        }

        return PartialView("~/Components/PageBuilder/Widgets/Registration/_RegisterForm.cshtml", model);
    }

    /// <summary>
    /// The widget properties posted with the form. Missing or unreadable: the defaults.
    /// </summary>
    private RegisterWidgetProperties GetWidgetProperties()
    {
        try
        {
            return HttpContext.GetWidgetProperties<RegisterWidgetProperties>(propertiesSerializer) ?? new();
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or JsonException)
        {
            logger.LogWarning(new EventId(0, "REGISTER_WIDGET_PROPERTIES"), ex, "Registration form posted unreadable widget properties");
            return new();
        }
    }
}
