using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Resources;
using Kentico.Community.Portal.Web.Features.Registration;
using Htmx;
using Kentico.Community.Portal.Web.Membership;
using CMS.Core;
using System.Web;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Kentico.Community.Portal.Web.Features.Authentication;

[Route("[controller]/[action]")]
public class AuthenticationController : Controller
{
    private readonly SignInManager<CommunityMember> signInManager;
    private readonly UserManager<CommunityMember> userManager;
    private readonly WebPageMetaService metaService;
    private readonly IStringLocalizer<SharedResources> localizer;
    private readonly IEventLogService log;
    private readonly MemberContactManager contactManager;

    public AuthenticationController(
        SignInManager<CommunityMember> signInManager,
        UserManager<CommunityMember> userManager,
        WebPageMetaService metaService,
        IStringLocalizer<SharedResources> localizer,
        IEventLogService log,
        MemberContactManager contactManager)
    {
        this.localizer = localizer;
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.log = log;
        this.contactManager = contactManager;
        this.metaService = metaService;
    }

    [HttpGet]
    public ActionResult Login()
    {
        metaService.SetMeta(new("Sign In", "Sign in to the Kentico Community Portal"));

        return View("~/Features/Authentication/Login.cshtml", new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("~/Features/Authentication/_LoginForm.cshtml", model);
        }

        var signInResult = SignInResult.Failed;

        try
        {
            var member = await GetMember();

            if (member is null)
            {
                signInResult = SignInResult.Failed;
            }
            else if (!member.Enabled)
            {
                var emailConfirmationModel = new EmailConfirmationViewModel()
                {
                    State = EmailConfirmationState.Failure_NotYetConfirmed,
                    Message = localizer["This Email Is Not Verified Yet"],
                    SendButtonText = localizer["Send Verification Email"],
                    Username = member.UserName
                };

                return PartialView("~/Features/Registration/EmailConfirmation.cshtml", emailConfirmationModel);
            }
            else
            {
                signInResult = await signInManager.PasswordSignInAsync(member.UserName, model.Password, model.StaySignedIn, false);
            }

            if (signInResult.Succeeded)
            {
                _ = contactManager.SetMemberAsCurrentContact(member);
            }
        }
        catch (Exception ex)
        {
            log.LogException(nameof(AuthenticationController), nameof(Login), ex);

            signInResult = SignInResult.Failed;
        }

        if (!signInResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, localizer["Your sign-in attempt was not successful. Please try again."].ToString());

            return PartialView("~/Features/Authentication/_LoginForm.cshtml", model);
        }

        string decodedReturnUrl = HttpUtility.UrlDecode(returnUrl);

        string redirectUrl = !string.IsNullOrEmpty(decodedReturnUrl) && Url.IsLocalUrl(decodedReturnUrl)
            ? decodedReturnUrl
            : "/";

        Response.Htmx(h => h.Redirect(redirectUrl));

        return Request.IsHtmx()
            ? Ok()
            : Redirect(redirectUrl);

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

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        _ = contactManager.ResetCurrentContact();

        return Redirect("/");
    }
}
