using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Features.Registration;

[Route("[controller]/[action]")]
public class RegistrationController(
    WebPageMetaService metaService,
    SignInManager<CommunityMember> signInManager,
    UserManager<CommunityMember> userManager,
    CaptchaValidator captchaValidator,
    IStringLocalizer<SharedResources> localizer,
    IOptions<SystemEmailOptions> systemEmailOptions,
    IEventLogService log,
    IEmailService emailService,
    IInfoProvider<ChannelInfo> channelProvider,
    IWebsiteChannelContext channelContext,
    ConsentManager consentManager) : Controller
{
    private readonly WebPageMetaService metaService = metaService;
    private readonly SignInManager<CommunityMember> signInManager = signInManager;
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly SystemEmailOptions systemEmailOptions = systemEmailOptions.Value;
    private readonly CaptchaValidator captchaValidator = captchaValidator;
    private readonly IStringLocalizer<SharedResources> localizer = localizer;
    private readonly IEventLogService log = log;
    private readonly IEmailService emailService = emailService;
    private readonly IInfoProvider<ChannelInfo> channelProvider = channelProvider;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly ConsentManager consentManager = consentManager;

    [HttpGet]
    public ActionResult Register()
    {
        metaService.SetMeta(new("Register", "Register for a new account on the Kentico Community Portal"));

        return View("~/Features/Registration/Register.cshtml", new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        var captchaResponse = await captchaValidator.ValidateCaptcha(model);

        if (!captchaResponse.IsSuccess)
        {
            ModelState.AddModelError("", "Captcha is invalid");
        }

        if (!ModelState.IsValid)
        {
            model.CaptchaToken = "";
            return PartialView("~/Features/Registration/_RegisterForm.cshtml", model);
        }

        var member = new CommunityMember
        {
            UserName = model.UserName,
            Email = model.Email,
            //We need to set Enabled to false because kentico uses enabled to set email confirmation (not .EmailConfirmed)
            Enabled = false
        };

        var registerResult = IdentityResult.Failed();

        try
        {
            registerResult = await userManager.CreateAsync(member, model.Password);
        }
        catch (Exception ex)
        {
            log.LogException(nameof(RegistrationController), nameof(Register), ex);
            registerResult = IdentityResult.Failed([new() { Code = "Failure", Description = "Your registration was not successful." }]);
        }

        if (!registerResult.Succeeded)
        {
            foreach (string error in registerResult.Errors.Select(e => e.Description))
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return PartialView("~/Features/Registration/_RegisterForm.cshtml", model);
        }

        await SendVerificationEmail(member);

        if (model.ConsentAgreement)
        {
            await consentManager.AgreeToMarketingConsent();
        }

        return PartialView("~/Features/Registration/_VerifyEmail.cshtml");
    }

    [HttpGet]
    public async Task<ActionResult> ConfirmEmail([FromQuery] string memberEmail, [FromQuery] string confirmToken)
    {
        IdentityResult confirmResult;

        var user = await userManager.FindByEmailAsync(memberEmail);

        if (user is null)
        {
            return View("~/Features/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel()
            {
                State = EmailConfirmationState.Failure_ConfirmationFailed,
                Message = localizer["Email Confirmation failed"],
                SendButtonText = localizer["Send Again"],
                Username = ""
            });
        }

        if (user.Enabled)
        {
            return View("~/Features/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel
            {
                State = EmailConfirmationState.Success_AlreadyConfirmed,
                Message = localizer["Your email is already verified"]
            });
        }

        try
        {
            //Changes Enabled property of the user
            confirmResult = await userManager.ConfirmEmailAsync(user, confirmToken);
        }
        catch (InvalidOperationException)
        {
            confirmResult = IdentityResult.Failed(new IdentityError() { Description = "User not found." });
        }

        if (confirmResult.Succeeded)
        {
            return View("~/Features/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel
            {
                State = EmailConfirmationState.Success_Confirmed,
                Message = localizer["Email Confirmed"]
            });
        }

        return View("~/Features/Registration/EmailConfirmation.cshtml", new EmailConfirmationViewModel()
        {
            State = EmailConfirmationState.Failure_ConfirmationFailed,
            Message = localizer["Email Confirmation failed"],
            SendButtonText = localizer["Send Again"],
            Username = user.UserName!
        });
    }

    [HttpPost]
    public async Task<ActionResult> ResendVerificationEmail([FromQuery] string username)
    {
        var member = await userManager.FindByNameAsync(username);

        if (member is null)
        {
            return PartialView("~/Features/Registration/_VerifyEmail.cshtml");
        }

        await SendVerificationEmail(member);

        return PartialView("~/Features/Registration/_VerifyEmail.cshtml");
    }

    private async Task SendVerificationEmail(CommunityMember member)
    {
        var channel = await channelProvider.GetAsync(channelContext.WebsiteChannelName);
        string confirmToken = await userManager.GenerateEmailConfirmationTokenAsync(member);
        string confirmationURL = Url.Action(nameof(ConfirmEmail), "Registration",
        new
        {
            memberEmail = member.Email,
            confirmToken
        },
        Request.Scheme) ?? "";

        await emailService.SendEmail(new EmailMessage()
        {
            From = $"no-reply@{systemEmailOptions.SendingDomain}",
            Recipients = member.Email,
            Subject = $"Confirm your email for {channel.ChannelDisplayName}",
            Body = $"""
                <p>To confirm your email address, click <a data-confirmation-url href="{confirmationURL}">here</a>.</p>
                <p style="margin-bottom: 1rem;">You can also copy and paste this URL into your browser.</p>
                <p>{confirmationURL}</p>
                """
        });
    }
}

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
