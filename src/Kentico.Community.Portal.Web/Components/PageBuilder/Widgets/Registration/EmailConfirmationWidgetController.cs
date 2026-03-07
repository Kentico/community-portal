using CMS.Base;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;

[Route("[controller]/[action]")]
public class EmailConfirmationWidgetController(
    UserManager<CommunityMember> userManager,
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever webPageUrlRetriever,
    IMemberEmailService emailService,
    IReadOnlyModeProvider readOnlyProvider) : Controller
{
    [HttpPost]
    public async Task<ActionResult> ResendConfirmationEmail([FromQuery] string username)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return PartialView("~/Components/PageBuilder/Widgets/Registration/_VerifyEmail.cshtml", true);
        }

        var member = await userManager.FindByNameAsync(username);
        if (member is null)
        {
            await Task.Delay(Random.Shared.Next(2000, 7000));
            return PartialView("~/Components/PageBuilder/Widgets/Registration/_VerifyEmail.cshtml", true);
        }

        if (!await SendVerificationEmail(member))
        {
            ModelState.AddModelError("", "We were unable to generate a confirmation email. Please contact support.");

            return PartialView("~/Components/PageBuilder/Widgets/AccountRecovery/_VerifyEmail.cshtml", false);
        }

        return PartialView("~/Components/PageBuilder/Widgets/Registration/_VerifyEmail.cshtml", true);
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
