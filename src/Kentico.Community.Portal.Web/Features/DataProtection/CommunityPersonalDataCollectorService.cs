using CMS.Activities;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.EmailEngine;
using CMS.EmailLibrary;
using CMS.EmailMarketing;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.DataProtection;

/// <summary>
/// Gathers everything the portal stores about a <see cref="DataSubject"/> for the Data protection
/// app (Right to access, Data portability, and the Right to be forgotten preview).
/// </summary>
public class CommunityPersonalDataCollectorService(
    IInfoProvider<MemberRoleMemberInfo> memberRoleMemberProvider,
    IInfoProvider<MemberRoleInfo> memberRoleProvider,
    IInfoProvider<MemberExternalLoginInfo> externalLoginProvider,
    IInfoProvider<QAndAAnswerDataInfo> answerProvider,
    IInfoProvider<QAndAQuestionReactionInfo> questionReactionProvider,
    IInfoProvider<QAndAAnswerReactionInfo> answerReactionProvider,
    IInfoProvider<DiscussionMemberSubscriptionInfo> discussionSubscriptionProvider,
    IInfoProvider<DiscussionMemberNotificationSettingsInfo> notificationSettingsProvider,
    IInfoProvider<MemberBadgeMemberInfo> badgeMemberProvider,
    IInfoProvider<MemberBadgeInfo> badgeProvider,
    IInfoProvider<ActivityInfo> activityProvider,
    IInfoProvider<ConsentAgreementInfo> agreementProvider,
    IInfoProvider<ConsentInfo> consentProvider,
    IInfoProvider<ContactGroupMemberInfo> contactGroupMemberProvider,
    IInfoProvider<ContactGroupInfo> contactGroupProvider,
    IInfoProvider<EmailSubscriptionConfirmationInfo> confirmationProvider,
    IInfoProvider<EmailInfo> emailProvider,
    IInfoProvider<EmailStatisticsHitsInfo> hitsProvider,
    MemberContentItemsQuery contentItemsQuery,
    CommunityFormDataService formDataService)
{
    /// <summary>Member columns that hold personal data. Password hash and security stamp are left out.</summary>
    private static readonly (string Column, string Label)[] memberColumns =
    [
        ("MemberName", "User name"),
        ("MemberEmail", "Email"),
        ("MemberFirstName", "First name"),
        ("MemberLastName", "Last name"),
        ("MemberCreated", "Created"),
        ("MemberEnabled", "Enabled"),
        ("MemberEmailConfirmedDate", "Email confirmed"),
        ("MemberModerationStatus", "Moderation status"),
        ("MemberLinkedInIdentifier", "LinkedIn"),
        ("MemberEmployerLink", "Employer"),
        ("MemberJobTitle", "Job title"),
        ("MemberCountry", "Country"),
        ("MemberTimeZone", "Time zone"),
        ("MemberBio", "Bio"),
        ("MemberAvatarFileExtension", "Avatar file type"),
        ("MemberAdministratorNotes", "Administrator notes"),
        ("MemberTermsAcceptedVersion", "Terms version accepted"),
        ("MemberTermsAcceptedDate", "Terms accepted"),
        ("MemberNewsletterOptInRequestedDate", "Newsletter requested at registration"),
        ("MemberAlternateEmail", "Alternate email"),
        ("MemberAlternateEmailVerifiedDate", "Alternate email verified"),
    ];

    private static readonly (string Column, string Label)[] contactColumns =
    [
        ("ContactGUID", "Contact GUID"),
        ("ContactEmail", "Email"),
        ("ContactFirstName", "First name"),
        ("ContactMiddleName", "Middle name"),
        ("ContactLastName", "Last name"),
        ("ContactJobTitle", "Job title"),
        ("ContactCompanyName", "Company"),
        ("ContactCity", "City"),
        ("ContactNotes", "Notes"),
        ("ContactCreated", "Created"),
        ("ContactLastModified", "Last modified"),
    ];

    public PersonalDataReport Collect(DataSubject subject)
    {
        var report = new PersonalDataReport();
        var memberIDs = subject.MemberIDs;
        var contactIDs = subject.ContactIDs;

        report.Add("Member", subject.Members.Select(m => Columns(m, memberColumns)));
        AddMemberRelated(report, memberIDs);
        AddForms(report, subject);
        AddContacts(report, subject.Contacts, contactIDs);
        AddEmailStatistics(report, subject.Emails);

        return report;
    }

    private void AddMemberRelated(PersonalDataReport report, IReadOnlyList<int> memberIDs)
    {
        if (memberIDs.Count == 0)
        {
            return;
        }

        var roles = memberRoleProvider.Get().AsEnumerable().ToDictionary(r => r.MemberRoleID, r => r.MemberRoleDisplayName);
        report.Add("Member roles", memberRoleMemberProvider.Get()
            .WhereIn(nameof(MemberRoleMemberInfo.MemberRoleMemberMemberID), memberIDs.ToList())
            .AsEnumerable()
            .Select(r => Row(("Role", roles.GetValueOrDefault(r.MemberRoleMemberMemberRoleID)))));

        report.Add("External logins", externalLoginProvider.Get()
            .WhereIn(nameof(MemberExternalLoginInfo.MemberExternalLoginMemberID), memberIDs.ToList())
            .AsEnumerable()
            .Select(l => Row(("Provider", l.MemberExternalLoginLoginProvider))));

        report.Add("Content items", contentItemsQuery.Get(memberIDs)
            .Select(i => Row(
                ("Content type", i.ContentTypeName),
                ("Name", i.DisplayName),
                ("Language", i.LanguageName),
                ("Content item ID", i.ContentItemID))));

        report.Add("Q&A answers", answerProvider.Get()
            .WhereIn(nameof(QAndAAnswerDataInfo.QAndAAnswerDataAuthorMemberID), memberIDs.ToList())
            .AsEnumerable()
            .Select(a => Row(
                ("Created", a.QAndAAnswerDataDateCreated),
                ("Modified", a.QAndAAnswerDataDateModified),
                ("Question page ID", a.QAndAAnswerDataQuestionWebPageItemID),
                ("Content", a.QAndAAnswerDataContent))));

        report.Add("Q&A question reactions", questionReactionProvider.Get()
            .WhereIn(nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionMemberID), memberIDs.ToList())
            .AsEnumerable()
            .Select(r => Row(
                ("Question page ID", r.QAndAQuestionReactionWebPageItemID),
                ("Reaction", r.QAndAQuestionReactionType),
                ("Date", r.QAndAQuestionReactionDateModified))));

        report.Add("Q&A answer reactions", answerReactionProvider.Get()
            .WhereIn(nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionMemberID), memberIDs.ToList())
            .AsEnumerable()
            .Select(r => Row(
                ("Answer ID", r.QAndAAnswerReactionAnswerID),
                ("Reaction", r.QAndAAnswerReactionType),
                ("Date", r.QAndAAnswerReactionDateModified))));

        report.Add("Discussion subscriptions", discussionSubscriptionProvider.Get()
            .WhereIn(nameof(DiscussionMemberSubscriptionInfo.DiscussionMemberSubscriptionMemberID), memberIDs.ToList())
            .AsEnumerable()
            .Select(s => Row(
                ("Question page ID", s.DiscussionMemberSubscriptionWebPageItemID),
                ("Date", s.DiscussionMemberSubscriptionDateModified))));

        report.Add("Discussion notification settings", notificationSettingsProvider.Get()
            .WhereIn(nameof(DiscussionMemberNotificationSettingsInfo.DiscussionMemberNotificationSettingsMemberID), memberIDs.ToList())
            .AsEnumerable()
            .Select(s => Row(
                ("Frequency", s.DiscussionMemberNotificationSettingsFrequencyType),
                ("Auto-subscribe", s.DiscussionMemberNotificationSettingsAutoSubscribeEnabled),
                ("Modified", s.DiscussionMemberNotificationSettingsDateModified))));

        var badges = badgeProvider.Get().AsEnumerable().ToDictionary(b => b.MemberBadgeID, b => b.MemberBadgeDisplayName);
        report.Add("Badges", badgeMemberProvider.Get()
            .WhereIn(nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberId), memberIDs.ToList())
            .AsEnumerable()
            .Select(b => Row(
                ("Badge", badges.GetValueOrDefault(b.MemberBadgeMemberMemberBadgeId)),
                ("Awarded", b.MemberBadgeMemberCreatedDate),
                ("Shown on profile", b.MemberBadgeMemberIsSelected))));
    }

    private void AddForms(PersonalDataReport report, DataSubject subject)
    {
        foreach (var form in formDataService.GetForms().Where(f => f.Shape.HasPersonalIdentifiers))
        {
            var items = formDataService.FindItems(form, subject.Emails, subject.MemberIDs);

            report.Add($"Form: {form.Form.FormDisplayName}", items.Select(item =>
                (IReadOnlyDictionary<string, object?>)item.ColumnNames
                    .Where(c => !c.EndsWith("ID", StringComparison.Ordinal) || string.Equals(c, form.Shape.MemberIDColumn, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(c => c, c => item.GetValue(c))));
        }
    }

    private void AddContacts(PersonalDataReport report, IReadOnlyList<ContactInfo> contacts, IReadOnlyList<int> contactIDs)
    {
        report.Add("Contacts", contacts.Select(c => Columns(c, contactColumns)));

        if (contactIDs.Count == 0)
        {
            return;
        }

        report.Add("Activities", activityProvider.Get()
            .WhereIn(nameof(ActivityInfo.ActivityContactID), contactIDs.ToList())
            .OrderBy(nameof(ActivityInfo.ActivityCreated))
            .AsEnumerable()
            .Select(a => Row(
                ("Type", a.ActivityType),
                ("Title", a.ActivityTitle),
                ("URL", a.ActivityURL),
                ("Date", a.ActivityCreated))));

        var consents = consentProvider.Get().AsEnumerable().ToDictionary(c => c.ConsentID, c => c.ConsentDisplayName);
        report.Add("Consent agreements", agreementProvider.Get()
            .WhereIn(nameof(ConsentAgreementInfo.ConsentAgreementContactID), contactIDs.ToList())
            .OrderBy(nameof(ConsentAgreementInfo.ConsentAgreementTime))
            .AsEnumerable()
            .Select(a => Row(
                ("Consent", consents.GetValueOrDefault(a.ConsentAgreementConsentID)),
                ("Revoked", a.ConsentAgreementRevoked),
                ("Date", a.ConsentAgreementTime))));

        var lists = contactGroupProvider.Get()
            .WhereTrue(nameof(ContactGroupInfo.ContactGroupIsRecipientList))
            .AsEnumerable()
            .ToDictionary(g => g.ContactGroupID, g => g.ContactGroupDisplayName);

        report.Add("Email subscriptions", confirmationProvider.Get()
            .WhereIn(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID), contactIDs.ToList())
            .OrderBy(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationDate))
            .AsEnumerable()
            .Select(c => Row(
                ("Recipient list", lists.GetValueOrDefault(c.EmailSubscriptionConfirmationRecipientListID)),
                ("Subscribed", c.EmailSubscriptionConfirmationIsApproved),
                ("Date", c.EmailSubscriptionConfirmationDate))));

        report.Add("Recipient list memberships", contactGroupMemberProvider.Get()
            .WhereIn(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contactIDs.ToList())
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
            .AsEnumerable()
            .Where(m => lists.ContainsKey(m.ContactGroupMemberContactGroupID))
            .Select(m => Row(("Recipient list", lists[m.ContactGroupMemberContactGroupID]))));
    }

    /// <summary>
    /// Sent email copies (kept for the email queue archive period) and the open/click hits that can
    /// still be linked to them through the mailout GUID.
    /// </summary>
    private void AddEmailStatistics(PersonalDataReport report, IReadOnlyList<string> emails)
    {
        var sent = FindSentEmails(emailProvider, emails);
        report.Add("Sent emails", sent.Select(e => Row(
            ("Subject", e.EmailSubject),
            ("To", e.EmailTo),
            ("Created", e.EmailCreated))));

        var mailoutGuids = sent.Select(e => e.EmailMailoutGuid).Where(g => g != Guid.Empty).Distinct().ToList();
        if (mailoutGuids.Count == 0)
        {
            return;
        }

        report.Add("Email opens and clicks", hitsProvider.Get()
            .WhereIn(nameof(EmailStatisticsHitsInfo.EmailStatisticsHitsMailoutGUID), mailoutGuids)
            .OrderBy(nameof(EmailStatisticsHitsInfo.EmailStatisticsHitsTime))
            .AsEnumerable()
            .Select(h => Row(
                ("Type", h.EmailStatisticsHitsType),
                ("Date", h.EmailStatisticsHitsTime))));
    }

    internal static IReadOnlyList<EmailInfo> FindSentEmails(IInfoProvider<EmailInfo> emailProvider, IReadOnlyList<string> emails) =>
        emails.Count == 0
            ? []
            : [.. emailProvider.Get().WhereIn(nameof(EmailInfo.EmailTo), emails.ToList()).AsEnumerable()];

    private static IReadOnlyDictionary<string, object?> Columns(BaseInfo info, (string Column, string Label)[] columns)
    {
        var row = new Dictionary<string, object?>();

        foreach (var (column, label) in columns)
        {
            if (info.TryGetValue(column, out object? value))
            {
                row[label] = value;
            }
        }

        return row;
    }

    private static IReadOnlyDictionary<string, object?> Row(params (string Key, object? Value)[] values) =>
        values.ToDictionary(v => v.Key, v => v.Value);
}
