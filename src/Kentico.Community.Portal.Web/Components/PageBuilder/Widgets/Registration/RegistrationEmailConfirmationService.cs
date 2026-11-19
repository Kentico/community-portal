using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;

public enum RegistrationConfirmResult
{
    Confirmed,
    /// <summary>The member is enabled already.</summary>
    AlreadyConfirmed,
    UnderModeration,
    /// <summary>Unknown member, a disabled member (confirmed before), or an invalid / expired token.</summary>
    Failed,
}

/// <summary>
/// Registration email confirmation: sending the link and confirming it.
/// Only a member who never confirmed their email (<see cref="CommunityMember.CanConfirmEmail"/>) can confirm.
/// </summary>
public class RegistrationEmailConfirmationService(
    UserManager<CommunityMember> userManager,
    IContentRetriever contentRetriever,
    IWebPageUrlRetriever webPageUrlRetriever,
    IMemberEmailService emailService,
    RecipientListManager recipientListManager,
    IInfoProvider<ContactGroupInfo> contactGroupProvider,
    IInfoProvider<MemberInfo> memberProvider,
    IReadOnlyModeProvider readOnlyProvider,
    TimeProvider timeProvider,
    ILogger<RegistrationEmailConfirmationService> logger)
{
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly IWebPageUrlRetriever webPageUrlRetriever = webPageUrlRetriever;
    private readonly IMemberEmailService emailService = emailService;
    private readonly RecipientListManager recipientListManager = recipientListManager;
    private readonly IInfoProvider<ContactGroupInfo> contactGroupProvider = contactGroupProvider;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ILogger<RegistrationEmailConfirmationService> logger = logger;

    /// <returns>False when the confirmation page doesn't exist.</returns>
    public async Task<bool> SendConfirmationEmail(CommunityMember member)
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

        await emailService.SendEmail(member, MemberEmailConfiguration.RegistrationConfirmation(confirmationURL, member.NewsletterOptInRequestedDate is not null));

        return true;
    }

    public async Task<CommunityMember?> FindMember(string memberEmail) =>
        string.IsNullOrWhiteSpace(memberEmail)
            ? null
            : await userManager.FindByEmailAsync(memberEmail.Trim());

    /// <summary>
    /// Enables the member, sets <see cref="CommunityMember.EmailConfirmedDate"/>, grants the KenticoEmployee role
    /// for an @kentico.com email, and completes the newsletter opt-in.
    /// </summary>
    public async Task<(RegistrationConfirmResult Result, CommunityMember? Member)> ConfirmEmail(string memberEmail, string confirmToken)
    {
        var member = await FindMember(memberEmail);
        if (member is null || string.IsNullOrWhiteSpace(confirmToken))
        {
            return (RegistrationConfirmResult.Failed, member);
        }

        switch (member.SignInState)
        {
            case MemberSignInState.UnderModeration:
                return (RegistrationConfirmResult.UnderModeration, member);
            case MemberSignInState.Active:
                return (RegistrationConfirmResult.AlreadyConfirmed, member);
            case MemberSignInState.Disabled:
                return (RegistrationConfirmResult.Failed, member);
            case MemberSignInState.EmailNotConfirmed:
            default:
                break;
        }

        // Saved with the confirmation below, only if the token is valid
        member.EmailConfirmedDate = timeProvider.GetLocalNow().DateTime;

        IdentityResult result;
        try
        {
            // Also sets Enabled (Xperience's EmailConfirmed)
            result = await userManager.ConfirmEmailAsync(member, confirmToken);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(new EventId(0, "REGISTRATION_CONFIRM_FAILED"), ex, "Email confirmation failed for member {MemberID}", member.Id);
            result = IdentityResult.Failed(new IdentityError { Description = "User not found." });
        }

        if (!result.Succeeded)
        {
            member.EmailConfirmedDate = null;
            return (RegistrationConfirmResult.Failed, member);
        }

        await GrantKenticoEmployeeRoleIfInternalEmployee(member);
        await CompleteNewsletterOptIn(member);

        return (RegistrationConfirmResult.Confirmed, member);
    }

    /// <summary>
    /// Subscribes the member to the newsletter if they asked for it at registration
    /// (<see cref="CommunityMember.NewsletterOptInRequestedDate"/>), then clears the request.
    /// <see cref="RecipientListManager.Subscribe"/> also agrees to the Email marketing consent, whose
    /// text labelled the registration checkbox. Confirming proves the member owns the address, so no
    /// second (newsletter) confirmation is sent.
    /// Idempotent: Subscribe skips an existing subscription and agreement, and the request is cleared
    /// only after it. Read-only mode: confirmation itself fails (no writes), so this isn't reached; the
    /// check is a guard.
    /// </summary>
    private async Task CompleteNewsletterOptIn(CommunityMember member)
    {
        if (member.NewsletterOptInRequestedDate is null || readOnlyProvider.IsReadOnly)
        {
            return;
        }

        var newsletter = (await contactGroupProvider.Get()
            .WhereEquals(nameof(ContactGroupInfo.ContactGroupName), RecipientListCodeNames.NEWSLETTER)
            .WhereEquals(nameof(ContactGroupInfo.ContactGroupIsRecipientList), true)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (newsletter is null)
        {
            logger.LogError(
                new EventId(0, "NEWSLETTER_OPT_IN_FAILED"),
                "Recipient list {RecipientList} not found. Member {MemberId} wasn't subscribed.",
                RecipientListCodeNames.NEWSLETTER,
                member.Id);
            return;
        }

        await recipientListManager.Subscribe(member.Email!, newsletter.ContactGroupID);

        /*
         * Clear only this column. Saving the whole member (UserManager.UpdateAsync) would write back the
         * security stamp loaded before confirmation, undoing the rotation MemberSecurityStampRotation did
         */
        var memberInfo = await memberProvider.GetAsync(member.Id);
        if (memberInfo is not null)
        {
            memberInfo.MemberNewsletterOptInRequestedDate = null;
            await memberProvider.SetAsync(memberInfo);
        }
    }

    /// <summary>
    /// Grants the KenticoEmployee Member Role the moment a member confirms an @kentico.com email.
    /// Confirmation is itself the trust signal - no matching CMS admin User is required.
    /// See docs/adr/0003-kenticoemployee-role-lifecycle.md.
    /// </summary>
    private async Task GrantKenticoEmployeeRoleIfInternalEmployee(CommunityMember member)
    {
        if (!member.IsInternalEmployee)
        {
            return;
        }

        if (await userManager.IsInRoleAsync(member, SystemMemberRoles.KenticoEmployee.MemberRoleName))
        {
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(member, SystemMemberRoles.KenticoEmployee.MemberRoleName);
        if (!roleResult.Succeeded)
        {
            logger.LogError(
                new EventId(0, "GRANT_KENTICO_EMPLOYEE_ROLE_FAILED"),
                "Failed to grant KenticoEmployee role to member {MemberId}: {Errors}",
                member.Id,
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }
    }
}
