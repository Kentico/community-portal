using CMS.Base;
using Kentico.Community.Portal.Web.Features.Members;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;

[Route("[controller]/[action]")]
public class EmailConfirmationWidgetController(
    UserManager<CommunityMember> userManager,
    RegistrationEmailConfirmationService confirmationService,
    IReadOnlyModeProvider readOnlyProvider,
    ILogger<EmailConfirmationWidgetController> logger) : Controller
{
    private const string VERIFY_EMAIL_VIEW_PATH = "~/Components/PageBuilder/Widgets/Registration/_VerifyEmail.cshtml";

    /// <summary>
    /// Always the same response, and roughly the same response time, whether or not anything was sent:
    /// it reveals nothing about the member. Sends only to a member who never confirmed their email
    /// and isn't moderated (<see cref="CommunityMember.CanConfirmEmail"/>).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(MemberRateLimitingConstants.EmailVerification)]
    public async Task<ActionResult> ResendConfirmationEmail([FromQuery] string? username)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return PartialView(VERIFY_EMAIL_VIEW_PATH, true);
        }

        // Started first: lookup + send and "nothing to do" both take this long
        var minimumDuration = Task.Delay(Random.Shared.Next(2000, 4000));

        try
        {
            var member = string.IsNullOrWhiteSpace(username)
                ? null
                : await userManager.FindByNameAsync(username);

            if (member is { CanConfirmEmail: true } && !await confirmationService.SendConfirmationEmail(member))
            {
                logger.LogError(new EventId(0, "RESEND_CONFIRMATION_FAILED"), "The email confirmation page was not found. Member {MemberID} got no email.", member.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(new EventId(0, "RESEND_CONFIRMATION_FAILED"), ex, "Resending the confirmation email failed");
        }

        await minimumDuration;

        return PartialView(VERIFY_EMAIL_VIEW_PATH, true);
    }

    /// <summary>
    /// The confirm button on the confirmation page (<see cref="EmailConfirmationWidget"/>). Opening the link
    /// itself changes nothing.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ConfirmEmail([FromForm] string? memberEmail, [FromForm] string? confirmToken)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return PartialView(EmailConfirmationWidget.VIEW_PATH, new EmailConfirmationViewModel
            {
                State = EmailConfirmationState.Failure_ConfirmationFailed,
                Message = "The site is in read-only mode. Try the link again later.",
            });
        }

        var (result, member) = await confirmationService.ConfirmEmail(memberEmail ?? "", confirmToken ?? "");

        return result switch
        {
            RegistrationConfirmResult.Confirmed => PartialView(EmailConfirmationWidget.VIEW_PATH, EmailConfirmationViewModel.Confirmed()),
            RegistrationConfirmResult.AlreadyConfirmed => PartialView(EmailConfirmationWidget.VIEW_PATH, EmailConfirmationViewModel.AlreadyConfirmed()),
            RegistrationConfirmResult.UnderModeration => PartialView("~/Components/PageBuilder/Widgets/Registration/_ModerationStatus.cshtml"),
            _ => PartialView(EmailConfirmationWidget.VIEW_PATH, EmailConfirmationViewModel.Failed(member)),
        };
    }
}
