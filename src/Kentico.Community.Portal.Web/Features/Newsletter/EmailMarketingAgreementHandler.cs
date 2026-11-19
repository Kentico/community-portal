using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.EmailMarketing;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Core.Membership;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Features.Newsletter;

/// <summary>
/// Keeps the <see cref="ConsentCodeNames.EMAIL_MARKETING"/> agreement in step with a newsletter
/// subscription confirmation as it's written, including the double opt-in click Xperience handles
/// itself. <see cref="EmailMarketingAgreementReconciliationTask"/> catches
/// anything this misses.
/// </summary>
public class EmailMarketingAgreementHandler(
    IInfoProvider<ContactGroupInfo> contactGroupProvider,
    IInfoProvider<ContactInfo> contactProvider,
    IInfoProvider<EmailSubscriptionConfirmationInfo> confirmationProvider,
    EmailMarketingConsentManager consentManager,
    IOptions<EmailMarketingOptions> options)
{
    private readonly IInfoProvider<ContactGroupInfo> contactGroupProvider = contactGroupProvider;
    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;
    private readonly IInfoProvider<EmailSubscriptionConfirmationInfo> confirmationProvider = confirmationProvider;
    private readonly EmailMarketingConsentManager consentManager = consentManager;
    private readonly IOptions<EmailMarketingOptions> options = options;

    /// <summary>
    /// Inserted or updated → <see cref="EmailMarketingAgreementPolicy.Decide"/> on the contact's latest
    /// confirmation, not the saved row: Xperience updates old rows too (a contact merge re-points
    /// the merged contact's rows), and an old row says nothing about the contact's current state.
    /// </summary>
    public async Task OnSaved(EmailSubscriptionConfirmationInfo confirmation)
    {
        if (await GetNewsletterContact(confirmation) is not ContactInfo contact)
        {
            return;
        }

        var confirmations = await GetOtherConfirmations(contact, confirmation);

        switch (EmailMarketingAgreementPolicy.Decide([.. confirmations, ToConfirmation(confirmation)], options.Value.AgreementCutoffDate))
        {
            case EmailMarketingAgreementAction.Agree:
                _ = await consentManager.Agree(contact);
                break;
            case EmailMarketingAgreementAction.Revoke:
                _ = await consentManager.Revoke(contact);
                break;
            case EmailMarketingAgreementAction.None:
            default:
                break;
        }
    }

    /// <summary>
    /// Deleted → revoke, unless the contact's latest remaining confirmation is still approved. Never
    /// agrees: deleting a row isn't a subscription.
    /// </summary>
    public async Task OnDeleted(EmailSubscriptionConfirmationInfo confirmation)
    {
        if (await GetNewsletterContact(confirmation) is not ContactInfo contact)
        {
            return;
        }

        var remaining = await GetOtherConfirmations(contact, confirmation);

        if (EmailMarketingAgreementPolicy.Decide(remaining, options.Value.AgreementCutoffDate) == EmailMarketingAgreementAction.Revoke)
        {
            _ = await consentManager.Revoke(contact);
        }
    }

    /// <summary>
    /// The contact's other confirmations of the same list. The saved or deleted row is left out so
    /// its in-memory values count, whatever the database returns mid-write.
    /// </summary>
    private async Task<IReadOnlyList<NewsletterConfirmation>> GetOtherConfirmations(ContactInfo contact, EmailSubscriptionConfirmationInfo confirmation) =>
        [.. (await confirmationProvider.Get()
            .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID), contact.ContactID)
            .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationRecipientListID), confirmation.EmailSubscriptionConfirmationRecipientListID)
            .WhereNotEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationID), confirmation.EmailSubscriptionConfirmationID)
            .GetEnumerableTypedResultAsync())
            .Select(ToConfirmation)];

    private async Task<ContactInfo?> GetNewsletterContact(EmailSubscriptionConfirmationInfo confirmation)
    {
        var list = await contactGroupProvider.GetAsync(confirmation.EmailSubscriptionConfirmationRecipientListID);
        if (list is null || !string.Equals(list.ContactGroupName, RecipientListCodeNames.NEWSLETTER, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return await contactProvider.GetAsync(confirmation.EmailSubscriptionConfirmationContactID);
    }

    public static NewsletterConfirmation ToConfirmation(EmailSubscriptionConfirmationInfo info) =>
        new(
            info.EmailSubscriptionConfirmationID,
            info.EmailSubscriptionConfirmationContactID,
            info.EmailSubscriptionConfirmationDate,
            info.EmailSubscriptionConfirmationIsApproved);
}
