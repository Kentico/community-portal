using CMS.Base;
using Htmx;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Members.EmailVerification;

/// <summary>
/// Targets of the links emailed by <see cref="MemberEmailVerificationService"/>. Anonymous: the member
/// can click the link on any device, signed in or not. The token proves they own the email.
/// </summary>
/// <remarks>
/// Opening a link (GET) changes nothing: corporate email scanners pre-open links. It renders a confirm button
/// that POSTs the token (in the body, with the antiforgery token) to the same URL, which makes the change.
/// </remarks>
[Route("member-email")]
[AllowAnonymous]
public class MemberEmailController(
    MemberEmailVerificationService verificationService,
    SignInManager<CommunityMember> signInManager,
    WebPageMetaService metaService,
    IReadOnlyModeProvider readOnlyProvider) : Controller
{
    private const string VIEW_PATH = "~/Features/Members/EmailVerification/EmailVerificationResult.cshtml";
    private const string RESULT_PARTIAL_PATH = "~/Features/Members/EmailVerification/_EmailVerificationResult.cshtml";

    private readonly MemberEmailVerificationService verificationService = verificationService;
    private readonly SignInManager<CommunityMember> signInManager = signInManager;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    [HttpGet("confirm-change")]
    public ActionResult ConfirmEmailChange(int memberID, string? email, string? token) =>
        ConfirmPrompt(
            "Confirm your new email",
            nameof(ConfirmEmailChangePost),
            new EmailVerificationConfirmModel(memberID, email ?? "", token ?? ""),
            $"Confirm {email} as your account email.",
            "Confirm my new email");

    [HttpPost("confirm-change")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ConfirmEmailChangePost([FromForm] EmailVerificationConfirmModel model)
    {
        SetMeta("Confirm your new email");

        if (readOnlyProvider.IsReadOnly)
        {
            return ResultView(ReadOnlyResult());
        }

        var (result, member) = await verificationService.ConfirmEmailChange(model.MemberID, model.Email ?? "", model.Token ?? "");

        bool isSignedInMember = member is not null
            && CommunityMember.GetMemberIDFromClaim(HttpContext) == CommunityMemberID.From(member.Id);

        /*
         * The change rotated the security stamp, which signs out every session. Keep this one, with its
         * "stay signed in" choice (like ASP.NET Core Identity UI's ConfirmEmailChange).
         */
        if (result == EmailVerificationConfirmResult.Verified && isSignedInMember)
        {
            await signInManager.RefreshSignInAsync(member!);
        }

        return ResultView(result switch
        {
            EmailVerificationConfirmResult.Verified => new EmailVerificationResultViewModel(
                EmailVerificationOutcome.Verified,
                $"Your account email is now {member!.Email}. We let your previous email know about the change.",
                isSignedInMember),
            EmailVerificationConfirmResult.AlreadyVerified => new EmailVerificationResultViewModel(
                EmailVerificationOutcome.AlreadyVerified,
                "This email is already your account email.",
                isSignedInMember),
            EmailVerificationConfirmResult.InUse => new EmailVerificationResultViewModel(
                EmailVerificationOutcome.InUse,
                "This email is already used by another account. Your account email was not changed.",
                isSignedInMember),
            _ => InvalidLink(isSignedInMember, "Your account email was not changed."),
        });
    }

    [HttpGet("confirm-alternate")]
    public ActionResult ConfirmAlternateEmail(int memberID, string? email, string? token) =>
        ConfirmPrompt(
            "Verify your alternate email",
            nameof(ConfirmAlternateEmailPost),
            new EmailVerificationConfirmModel(memberID, email ?? "", token ?? ""),
            $"Confirm {email} as your alternate email.",
            "Verify my alternate email");

    [HttpPost("confirm-alternate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ConfirmAlternateEmailPost([FromForm] EmailVerificationConfirmModel model)
    {
        SetMeta("Verify your alternate email");

        if (readOnlyProvider.IsReadOnly)
        {
            return ResultView(ReadOnlyResult());
        }

        var result = await verificationService.ConfirmAlternateEmail(model.MemberID, model.Email ?? "", model.Token ?? "");

        bool isSignedInMember = CommunityMember.GetMemberIDFromClaim(HttpContext) == CommunityMemberID.From(model.MemberID);

        return ResultView(result switch
        {
            EmailVerificationConfirmResult.Verified => new EmailVerificationResultViewModel(
                EmailVerificationOutcome.Verified,
                "Your alternate email is verified.",
                isSignedInMember),
            EmailVerificationConfirmResult.AlreadyVerified => new EmailVerificationResultViewModel(
                EmailVerificationOutcome.AlreadyVerified,
                "Your alternate email is already verified.",
                isSignedInMember),
            EmailVerificationConfirmResult.InUse => new EmailVerificationResultViewModel(
                EmailVerificationOutcome.InUse,
                "This email is already used by another account, so it can't be your alternate email.",
                isSignedInMember),
            _ => InvalidLink(isSignedInMember, "Your alternate email is not verified."),
        });
    }

    private ViewResult ConfirmPrompt(string title, string postAction, EmailVerificationConfirmModel form, string message, string buttonText)
    {
        SetMeta(title);

        if (readOnlyProvider.IsReadOnly)
        {
            return View(VIEW_PATH, ReadOnlyResult());
        }

        return View(VIEW_PATH, new EmailVerificationResultViewModel(EmailVerificationOutcome.Pending, message, false)
        {
            Confirm = form,
            ConfirmAction = postAction,
            ConfirmButtonText = buttonText,
        });
    }

    /// <summary>htmx swaps the result into the page. Without JavaScript the form posts normally: the whole page.</summary>
    private ActionResult ResultView(EmailVerificationResultViewModel model) =>
        Request.IsHtmx()
            ? PartialView(RESULT_PARTIAL_PATH, model)
            : (ActionResult)View(VIEW_PATH, model);

    private static EmailVerificationResultViewModel InvalidLink(bool isSignedInMember, string notChangedMessage) =>
        new(
            EmailVerificationOutcome.Invalid,
            $"This link is invalid or has expired. {notChangedMessage} Request a new link from your account page.",
            isSignedInMember);

    private static EmailVerificationResultViewModel ReadOnlyResult() =>
        new(
            EmailVerificationOutcome.Invalid,
            "The site is in read-only mode. Try the link again later.",
            false);

    private void SetMeta(string title) =>
        metaService.SetMeta(new WebPageMeta(title, "Kentico Community Portal account email verification") { Robots = WebPageMeta.ROBOTS_NOINDEX });
}

public enum EmailVerificationOutcome
{
    /// <summary>The link was opened: the member still has to click the confirm button.</summary>
    Pending,
    Verified,
    AlreadyVerified,
    InUse,
    Invalid
}

/// <summary>Posted by the confirm button. Same values as the link's query string.</summary>
public record EmailVerificationConfirmModel(int MemberID, string? Email, string? Token);

/// <param name="IsSignedInMember">The visitor is signed in as the member the link is for: link to their account instead of sign-in.</param>
public record EmailVerificationResultViewModel(EmailVerificationOutcome Outcome, string Message, bool IsSignedInMember)
{
    public bool IsSuccess => Outcome is EmailVerificationOutcome.Verified or EmailVerificationOutcome.AlreadyVerified;

    /// <summary><see cref="EmailVerificationOutcome.Pending"/> only.</summary>
    public EmailVerificationConfirmModel? Confirm { get; init; }
    /// <summary>The POST action the confirm button targets.</summary>
    public string ConfirmAction { get; init; } = "";
    public string ConfirmButtonText { get; init; } = "";
}
