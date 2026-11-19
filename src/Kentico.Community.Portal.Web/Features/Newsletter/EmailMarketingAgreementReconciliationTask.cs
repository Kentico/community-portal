using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.EmailMarketing;
using CMS.Scheduler;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Core.Membership;
using Kentico.Community.Portal.Web.Features.Newsletter;
using Microsoft.Extensions.Options;

[assembly: RegisterScheduledTask(
    identifier: EmailMarketingAgreementReconciliationTask.IDENTIFIER,
    taskType: typeof(EmailMarketingAgreementReconciliationTask))]

namespace Kentico.Community.Portal.Web.Features.Newsletter;

/// <summary>
/// Safety net for <see cref="EmailMarketingAgreementHandler"/>. Idempotent:
/// <list type="bullet">
/// <item>newsletter subscribers confirmed on/after the cutoff without an agreement → agree;</item>
/// <item>agreements of contacts no longer subscribed → revoke.</item>
/// </list>
/// Run it before editing the Email marketing consent text, so pending confirmations are recorded
/// against the text they saw.
/// </summary>
public partial class EmailMarketingAgreementReconciliationTask(
    IInfoProvider<ContactGroupInfo> contactGroupProvider,
    IInfoProvider<ContactGroupMemberInfo> contactGroupMemberProvider,
    IInfoProvider<EmailSubscriptionConfirmationInfo> confirmationProvider,
    IInfoProvider<ConsentAgreementInfo> consentAgreementProvider,
    IInfoProvider<ContactInfo> contactProvider,
    EmailMarketingConsentManager consentManager,
    IReadOnlyModeProvider readOnlyProvider,
    IOptions<EmailMarketingOptions> options,
    TimeProvider clock,
    ILogger<EmailMarketingAgreementReconciliationTask> logger) : IScheduledTask
{
    public const string IDENTIFIER = "KenticoCommunity.EmailMarketingAgreementReconciliationTask";

    private readonly IInfoProvider<ContactGroupInfo> contactGroupProvider = contactGroupProvider;
    private readonly IInfoProvider<ContactGroupMemberInfo> contactGroupMemberProvider = contactGroupMemberProvider;
    private readonly IInfoProvider<EmailSubscriptionConfirmationInfo> confirmationProvider = confirmationProvider;
    private readonly IInfoProvider<ConsentAgreementInfo> consentAgreementProvider = consentAgreementProvider;
    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;
    private readonly EmailMarketingConsentManager consentManager = consentManager;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;
    private readonly IOptions<EmailMarketingOptions> options = options;
    private readonly TimeProvider clock = clock;
    private readonly ILogger<EmailMarketingAgreementReconciliationTask> logger = logger;

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Email marketing reconciliation: {Agreed} agreed, {Revoked} revoked.")]
    partial void LogResult(int agreed, int revoked);

    public async Task<ScheduledTaskExecutionResult> Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return ScheduledTaskExecutionResult.Success;
        }

        var list = (await contactGroupProvider.Get()
            .WhereEquals(nameof(ContactGroupInfo.ContactGroupName), RecipientListCodeNames.NEWSLETTER)
            .WhereTrue(nameof(ContactGroupInfo.ContactGroupIsRecipientList))
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .FirstOrDefault();
        if (list is null)
        {
            return new ScheduledTaskExecutionResult($"Recipient list {RecipientListCodeNames.NEWSLETTER} not found.");
        }

        if (await consentManager.GetConsent() is not ConsentInfo consent)
        {
            return new ScheduledTaskExecutionResult($"Consent {ConsentCodeNames.EMAIL_MARKETING} not found.");
        }

        var cutoff = options.Value.AgreementCutoffDate;

        /*
         * Agreements first. A subscribe writes the list membership and confirmation before the
         * agreement, so a contact agreed by now has its subscription in the reads below. Read the
         * other way round, a subscribe landing in between looks like "agreed, not subscribed"
         */
        var agreedContactIDs = await GetAgreedContactIDs(consent, cancellationToken);
        var confirmations = await GetConfirmations(list.ContactGroupID, contactID: null, cancellationToken);
        var listMembers = await GetListMemberIDs(list.ContactGroupID, contactID: null, cancellationToken);

        var plan = EmailMarketingAgreementPolicy.Reconcile(
            confirmations,
            listMembers,
            agreedContactIDs,
            cutoff,
            clock.GetLocalNow().DateTime);

        int agreed = 0;
        foreach (var contact in await GetContacts(plan.ContactIDsToAgree, cancellationToken))
        {
            agreed += await consentManager.Agree(contact) ? 1 : 0;
        }

        int revoked = 0;
        foreach (var contact in await GetContacts(plan.ContactIDsToRevoke, cancellationToken))
        {
            if (await IsStillUnsubscribed(contact.ContactID, list.ContactGroupID, cutoff, cancellationToken))
            {
                revoked += await consentManager.Revoke(contact) ? 1 : 0;
            }
        }

        if (agreed > 0 || revoked > 0)
        {
            LogResult(agreed, revoked);
        }

        return ScheduledTaskExecutionResult.Success;
    }

    /// <summary>
    /// Re-checks one contact just before its revoke, so a subscribe since the plan's reads keeps
    /// its agreement.
    /// </summary>
    private async Task<bool> IsStillUnsubscribed(int contactID, int listID, DateTime cutoff, CancellationToken cancellationToken)
    {
        var plan = EmailMarketingAgreementPolicy.Reconcile(
            await GetConfirmations(listID, contactID, cancellationToken),
            await GetListMemberIDs(listID, contactID, cancellationToken),
            new HashSet<int> { contactID },
            cutoff,
            clock.GetLocalNow().DateTime);

        return plan.ContactIDsToRevoke.Contains(contactID);
    }

    private async Task<List<NewsletterConfirmation>> GetConfirmations(int listID, int? contactID, CancellationToken cancellationToken)
    {
        var query = confirmationProvider.Get()
            .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationRecipientListID), listID);

        if (contactID is int id)
        {
            query = query.WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID), id);
        }

        return [.. (await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .Select(EmailMarketingAgreementHandler.ToConfirmation)];
    }

    private async Task<HashSet<int>> GetListMemberIDs(int listID, int? contactID, CancellationToken cancellationToken)
    {
        var query = contactGroupMemberProvider.Get()
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID), listID)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
            .Column(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID));

        if (contactID is int id)
        {
            query = query.WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), id);
        }

        return [.. (await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .Select(m => m.ContactGroupMemberRelatedID)];
    }

    /// <summary>
    /// Xperience adds a row per agree and revoke; a contact is agreed when its latest row isn't revoked.
    /// </summary>
    private async Task<HashSet<int>> GetAgreedContactIDs(ConsentInfo consent, CancellationToken cancellationToken)
    {
        var agreements = await consentAgreementProvider.Get()
            .WhereEquals(nameof(ConsentAgreementInfo.ConsentAgreementConsentID), consent.ConsentID)
            .Columns(
                nameof(ConsentAgreementInfo.ConsentAgreementID),
                nameof(ConsentAgreementInfo.ConsentAgreementContactID),
                nameof(ConsentAgreementInfo.ConsentAgreementRevoked),
                nameof(ConsentAgreementInfo.ConsentAgreementTime))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return [.. agreements
            .GroupBy(a => a.ConsentAgreementContactID)
            .Select(g => g
                .OrderByDescending(a => a.ConsentAgreementTime)
                .ThenByDescending(a => a.ConsentAgreementID)
                .First())
            .Where(a => !a.ConsentAgreementRevoked)
            .Select(a => a.ConsentAgreementContactID)];
    }

    private async Task<IReadOnlyList<ContactInfo>> GetContacts(IReadOnlyList<int> contactIDs, CancellationToken cancellationToken)
    {
        var contacts = new List<ContactInfo>();

        foreach (var batch in contactIDs.Chunk(500))
        {
            contacts.AddRange(await contactProvider.Get()
                .WhereIn(nameof(ContactInfo.ContactID), batch)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken));
        }

        return contacts;
    }
}
