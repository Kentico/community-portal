using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.EmailLibrary;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.DataProtection;

/// <summary>What an erasure changed, and the editorial content items left for a person to review.</summary>
public record ErasureResult(
    bool MemberAnonymized,
    int ContactsDeleted,
    int FormRowsDeleted,
    int FormRowsScrubbed,
    int SubscriptionsRemoved,
    int EmailStatisticsHitsUnlinked,
    int SentEmailsDeleted,
    IReadOnlyList<MemberContentItem> ContentItemsToReview);

/// <summary>
/// Right to be forgotten. Each step runs only when its
/// <see cref="ErasureOptionKeys"/> option is selected. Order matters: steps that find data by email
/// run before the member's email is replaced and before contacts are deleted.
/// </summary>
/// <remarks>
/// Nothing here catches failures: an exception reaches the Data protection app, which reports the
/// erasure as failed and logs the exception. Every step before the member's identity is replaced
/// finds its data again on a re-run and skips what's already gone, so the admin re-runs the erasure
/// after fixing the cause (see <see cref="MemberAnonymizer"/>).
/// </remarks>
public class CommunityPersonalDataEraserService(
    CommunityFormDataService formDataService,
    MemberAnonymizer memberAnonymizer,
    MemberContentItemsQuery contentItemsQuery,
    IInfoProvider<ContactInfo> contactProvider,
    IInfoProvider<ContactGroupInfo> contactGroupProvider,
    IInfoProvider<ContactGroupMemberInfo> contactGroupMemberProvider,
    IInfoProvider<DiscussionMemberSubscriptionInfo> discussionSubscriptionProvider,
    IInfoProvider<DiscussionMemberNotificationSettingsInfo> notificationSettingsProvider,
    IInfoProvider<EmailInfo> emailProvider,
    IInfoProvider<EmailStatisticsHitsInfo> hitsProvider,
    ILogger<CommunityPersonalDataEraserService> logger)
{
    /// <summary>Editorial content linked to a member. Erasure lists these; a person decides.</summary>
    private static readonly string[] reviewContentTypes =
    [
        MemberProfileContent.CONTENT_TYPE_NAME,
        AuthorContent.CONTENT_TYPE_NAME,
        TestimonialContent.CONTENT_TYPE_NAME,
        IntegrationContent.CONTENT_TYPE_NAME,
        LinkContent.CONTENT_TYPE_NAME,
    ];

    public async Task<ErasureResult> Erase(DataSubject subject, IDictionary<string, object> options, CancellationToken cancellationToken)
    {
        string placeholder = ErasureOptionKeys.ErasedEmail(subject.MemberIDs.Count == 1 ? subject.MemberIDs[0] : 0);

        var (hitsUnlinked, sentDeleted) = IsOn(options, ErasureOptionKeys.UNLINK_EMAIL_STATISTICS)
            ? UnlinkEmailStatistics(subject.Emails)
            : (0, 0);

        var (formDeleted, formScrubbed) = EraseForms(subject, options, placeholder);

        int subscriptionsRemoved = IsOn(options, ErasureOptionKeys.REMOVE_SUBSCRIPTIONS)
            ? RemoveSubscriptions(subject)
            : 0;

        int contactsDeleted = 0;
        if (IsOn(options, ErasureOptionKeys.DELETE_CONTACTS))
        {
            foreach (var contact in subject.Contacts)
            {
                contactProvider.Delete(contact);
                contactsDeleted++;
            }
        }

        var toReview = contentItemsQuery.Get(subject.MemberIDs, reviewContentTypes);

        bool anonymized = false;
        if (IsOn(options, ErasureOptionKeys.ANONYMIZE_MEMBER))
        {
            foreach (var member in subject.Members)
            {
                await memberAnonymizer.Anonymize(member, cancellationToken);
                anonymized = true;
            }
        }

        var result = new ErasureResult(anonymized, contactsDeleted, formDeleted, formScrubbed, subscriptionsRemoved,
            hitsUnlinked, sentDeleted, toReview);

        logger.LogInformation(
            "Personal data erased for members {MemberIDs}: {Result}",
            string.Join(",", subject.MemberIDs),
            result with { ContentItemsToReview = [] });

        if (toReview.Count > 0)
        {
            logger.LogWarning(
                "Personal data erasure left {Count} editorial content item(s) linked to members {MemberIDs} for manual review: {Items}",
                toReview.Count,
                string.Join(",", subject.MemberIDs),
                string.Join("; ", toReview.Select(i => $"{i.ContentTypeName} #{i.ContentItemID} '{i.DisplayName}'")));
        }

        return result;
    }

    private (int Deleted, int Scrubbed) EraseForms(DataSubject subject, IDictionary<string, object> options, string placeholder)
    {
        bool deleteShortLived = IsOn(options, ErasureOptionKeys.DELETE_SHORT_LIVED_FORM_DATA);
        bool scrub = IsOn(options, ErasureOptionKeys.SCRUB_FORM_EMAILS);
        if (!deleteShortLived && !scrub)
        {
            return (0, 0);
        }

        int deleted = 0;
        int scrubbed = 0;

        foreach (var form in formDataService.GetForms().Where(f => f.Shape.HasPersonalIdentifiers))
        {
            if (form.IsShortLived && deleteShortLived)
            {
                deleted += formDataService.DeleteItems(form, subject.Emails, subject.MemberIDs);
            }
            else if (scrub)
            {
                scrubbed += formDataService.ScrubEmails(form, subject.Emails, subject.MemberIDs, placeholder);
            }
        }

        return (deleted, scrubbed);
    }

    /// <summary>
    /// Removes recipient list memberships (the Email marketing agreement is revoked by the
    /// reconciliation task, or goes with the contact), and the member's Q&amp;A discussion
    /// subscriptions and notification settings.
    /// </summary>
    private int RemoveSubscriptions(DataSubject subject)
    {
        int removed = 0;

        if (subject.ContactIDs.Count > 0)
        {
            var listIDs = contactGroupProvider.Get()
                .WhereTrue(nameof(ContactGroupInfo.ContactGroupIsRecipientList))
                .Column(nameof(ContactGroupInfo.ContactGroupID))
                .AsEnumerable()
                .Select(g => g.ContactGroupID)
                .ToList();

            if (listIDs.Count > 0)
            {
                var memberships = contactGroupMemberProvider.Get()
                    .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
                    .WhereIn(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), subject.ContactIDs.ToList())
                    .WhereIn(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID), listIDs)
                    .ToList();

                foreach (var membership in memberships)
                {
                    contactGroupMemberProvider.Delete(membership);
                    removed++;
                }
            }
        }

        if (subject.MemberIDs.Count > 0)
        {
            foreach (var subscription in discussionSubscriptionProvider.Get()
                .WhereIn(nameof(DiscussionMemberSubscriptionInfo.DiscussionMemberSubscriptionMemberID), subject.MemberIDs.ToList())
                .ToList())
            {
                discussionSubscriptionProvider.Delete(subscription);
                removed++;
            }

            foreach (var settings in notificationSettingsProvider.Get()
                .WhereIn(nameof(DiscussionMemberNotificationSettingsInfo.DiscussionMemberNotificationSettingsMemberID), subject.MemberIDs.ToList())
                .ToList())
            {
                notificationSettingsProvider.Delete(settings);
                removed++;
            }
        }

        return removed;
    }

    /// <summary>
    /// Hit rows hold no address or contact. Their only link to a person is the mailout GUID, which
    /// joins to the sent email copy (<c>CMS_Email</c>, kept for the queue archive period). Blanking
    /// the GUID and deleting the copies keeps the hits for aggregate statistics only.
    /// </summary>
    private (int HitsUnlinked, int SentDeleted) UnlinkEmailStatistics(IReadOnlyList<string> emails)
    {
        var sent = CommunityPersonalDataCollectorService.FindSentEmails(emailProvider, emails);
        var mailoutGuids = sent.Select(e => e.EmailMailoutGuid).Where(g => g != Guid.Empty).Distinct().ToList();

        int hitsUnlinked = 0;
        if (mailoutGuids.Count > 0)
        {
            hitsUnlinked = hitsProvider.Get()
                .WhereIn(nameof(EmailStatisticsHitsInfo.EmailStatisticsHitsMailoutGUID), mailoutGuids)
                .Count;

            // The provider's validation rejects an empty mailout GUID, so this writes the column
            // directly. GUIDs are formatted from Guid values, never from input.
            string guids = string.Join(",", mailoutGuids.Select(g => $"'{g:D}'"));
            _ = ConnectionHelper.ExecuteNonQuery(
                $"UPDATE EmailLibrary_EmailStatisticsHits SET EmailStatisticsHitsMailoutGUID = '{Guid.Empty:D}' WHERE EmailStatisticsHitsMailoutGUID IN ({guids})",
                null,
                QueryTypeEnum.SQLQuery);
        }

        foreach (var email in sent)
        {
            emailProvider.Delete(email);
        }

        return (hitsUnlinked, sent.Count);
    }

    private static bool IsOn(IDictionary<string, object> options, string key) =>
        options.TryGetValue(key, out object? value) && ValidationHelper.GetBoolean(value, false);
}
