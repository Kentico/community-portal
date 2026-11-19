using CMS.Activities;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataEngine.Query;
using CMS.DataProtection;
using CMS.EmailEngine;
using CMS.EmailLibrary;
using CMS.EmailMarketing;
using CMS.Membership;
using CMS.OnlineForms;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Features.DataProtection;
using Kentico.Community.Portal.Web.Membership;
using Microsoft.AspNetCore.Identity;

namespace Kentico.Community.Portal.Web.Features.Testing;

/// <summary>
/// Seeds a member with one of each kind of personal data, runs the registered Data protection
/// collectors and erasers the way the admin app does, and reads back what is left.
/// Also sets form retention policies and seeds backdated form rows for the retention task.
/// </summary>
public class E2EDataProtectionTestingService(
    UserManager<CommunityMember> userManager,
    IInfoProvider<MemberExternalLoginInfo> externalLoginProvider,
    IInfoProvider<DiscussionMemberNotificationSettingsInfo> notificationSettingsProvider,
    IInfoProvider<MemberBadgeInfo> badgeProvider,
    IInfoProvider<MemberBadgeMemberInfo> badgeMemberProvider,
    IInfoProvider<ContactInfo> contactProvider,
    IInfoProvider<ActivityInfo> activityProvider,
    IInfoProvider<ConsentInfo> consentProvider,
    IConsentAgreementService consentAgreementService,
    IInfoProvider<ContactGroupInfo> contactGroupProvider,
    IInfoProvider<ContactGroupMemberInfo> contactGroupMemberProvider,
    IInfoProvider<EmailSubscriptionConfirmationInfo> confirmationProvider,
    IInfoProvider<EmailInfo> emailProvider,
    IInfoProvider<EmailStatisticsHitsInfo> hitsProvider,
    IInfoProvider<EmailConfigurationInfo> emailConfigurationProvider,
    IInfoProvider<MemberInfo> memberProvider,
    IInfoProvider<BizFormInfo> formProvider,
    IInfoProvider<FormRetentionPolicyInfo> policyProvider,
    CommunityFormDataService formDataService)
{
    /// <summary>Test-only password for seeded members. Not a real credential.</summary>
    public const string SEEDED_MEMBER_PASSWORD = "E2e-DataProtection-1";

    public const string NEWSLETTER_FORM = "NewsletterSignup";
    public const string FEEDBACK_FORM = "CommunityFeedbackForm";
    public const string MVP_ACTIVITY_FORM = "MVPActivity";

    public async Task<Result<E2EDataProtectionSeed>> SeedMember(E2EDataProtectionSeedRequest request)
    {
        string prefix = string.IsNullOrWhiteSpace(request.Prefix)
            ? $"e2e-dp-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(10_000)}"
            : request.Prefix;
        string email = $"{prefix}@kentico.local";

        var member = new CommunityMember
        {
            UserName = prefix,
            Email = email,
            EmailConfirmed = true,
            Enabled = true,
            Created = DateTime.Now,
            FirstName = "Erasure",
            LastName = "Subject",
            LinkedInIdentifier = prefix,
            JobTitle = "Developer",
            Country = "CZ",
            Bio = "Seeded for the data protection spec.",
            TimeZone = "Europe/Prague",
        };

        var created = await userManager.CreateAsync(member, SEEDED_MEMBER_PASSWORD);
        if (!created.Succeeded)
        {
            return Result.Failure<E2EDataProtectionSeed>(string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        int memberID = member.Id;

        await externalLoginProvider.SetAsync(new MemberExternalLoginInfo
        {
            MemberExternalLoginMemberID = memberID,
            MemberExternalLoginLoginProvider = "E2E",
            MemberExternalLoginIdentityKey = Guid.NewGuid().ToString(),
        });

        var notificationSettings = new DiscussionMemberNotificationSettingsInfo
        {
            DiscussionMemberNotificationSettingsMemberID = memberID,
            DiscussionMemberNotificationSettingsFrequencyType = "Never",
            DiscussionMemberNotificationSettingsAutoSubscribeEnabled = true,
            DiscussionMemberNotificationSettingsDateCreated = DateTime.Now,
            DiscussionMemberNotificationSettingsDateModified = DateTime.Now,
        };
        // The generated setter stores 0 as null; the column is NOT NULL with a default of 0
        notificationSettings.SetValue(nameof(DiscussionMemberNotificationSettingsInfo.DiscussionMemberNotificationSettingsLatestDiscussionEventID), 0);
        await notificationSettingsProvider.SetAsync(notificationSettings);

        if ((await badgeProvider.Get().TopN(1).GetEnumerableTypedResultAsync()).FirstOrDefault() is MemberBadgeInfo badge)
        {
            await badgeMemberProvider.SetAsync(new MemberBadgeMemberInfo
            {
                MemberBadgeMemberGUID = Guid.NewGuid(),
                MemberBadgeMemberMemberId = memberID,
                MemberBadgeMemberMemberBadgeId = badge.MemberBadgeID,
                MemberBadgeMemberCreatedDate = DateTime.Now,
                MemberBadgeMemberLastModified = DateTime.Now,
                MemberBadgeMemberIsSelected = true,
            });
        }

        var forms = formDataService.GetForms();
        var formRows = new List<E2ESeededFormRow>
        {
            InsertFormRow(forms, NEWSLETTER_FORM, new() { ["Email"] = email }),
            InsertFormRow(forms, FEEDBACK_FORM, new() { ["Email"] = email }),
            InsertFormRow(forms, MVP_ACTIVITY_FORM, new() { ["MemberID"] = memberID, ["MemberEmail"] = email }),
        };

        // Two contacts with the email: the member's and one from an earlier browser
        var contacts = new List<ContactInfo>();
        foreach (string lastName in new[] { "browser", "member" })
        {
            var contact = new ContactInfo { ContactEmail = email, ContactFirstName = "Erasure", ContactLastName = lastName };
            await contactProvider.SetAsync(contact);
            activityProvider.Set(new ActivityInfo
            {
                ActivityContactID = contact.ContactID,
                ActivityType = PredefinedActivityType.PAGE_VISIT,
                ActivityTitle = "E2E data protection visit",
                ActivityURL = "/",
                ActivityCreated = DateTime.Now,
            });
            contacts.Add(contact);
        }

        if (await consentProvider.GetAsync(ConsentCodeNames.WEBSITE_TRACKING) is ConsentInfo consent)
        {
            _ = consentAgreementService.Agree(contacts[0], consent);
        }

        int listMembershipID = await SubscribeToNewsletter(contacts[1]);
        var (sentEmailID, mailoutGuid, hitID) = await SeedEmailStatistics(email);

        return new E2EDataProtectionSeed(
            memberID, prefix, email, SEEDED_MEMBER_PASSWORD,
            [.. contacts.Select(c => c.ContactID)],
            formRows,
            listMembershipID,
            sentEmailID,
            mailoutGuid,
            hitID);
    }

    private E2ESeededFormRow InsertFormRow(IReadOnlyList<CommunityForm> forms, string formName, Dictionary<string, object> values)
    {
        var form = forms.FirstOrDefault(f => string.Equals(f.Form.FormName, formName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Form '{formName}' was not found.");

        var item = NewFormItem(form.ClassName, values);
        item.Insert();

        return new E2ESeededFormRow(form.Form.FormName, form.ClassName, item.ItemID);
    }

    private async Task<int> SubscribeToNewsletter(ContactInfo contact)
    {
        var list = (await contactGroupProvider.Get()
            .WhereEquals(nameof(ContactGroupInfo.ContactGroupName), E2EPrivacyTestingService.DEFAULT_RECIPIENT_LIST)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .First();

        var membership = new ContactGroupMemberInfo
        {
            ContactGroupMemberContactGroupID = list.ContactGroupID,
            ContactGroupMemberType = ContactGroupMemberTypeEnum.Contact,
            ContactGroupMemberRelatedID = contact.ContactID,
            ContactGroupMemberFromManual = true,
        };
        await contactGroupMemberProvider.SetAsync(membership);

        await confirmationProvider.SetAsync(new EmailSubscriptionConfirmationInfo
        {
            EmailSubscriptionConfirmationContactID = contact.ContactID,
            EmailSubscriptionConfirmationRecipientListID = list.ContactGroupID,
            EmailSubscriptionConfirmationIsApproved = true,
            EmailSubscriptionConfirmationDate = DateTime.Now,
        });

        return membership.ContactGroupMemberID;
    }

    /// <summary>A sent email copy (archived, so the queue never sends it) and one open hit linked by mailout GUID.</summary>
    private async Task<(int EmailID, Guid MailoutGuid, long HitID)> SeedEmailStatistics(string email)
    {
        var mailoutGuid = Guid.NewGuid();
        var sent = new EmailInfo
        {
            EmailFrom = "community@kentico.local",
            EmailTo = email,
            EmailSubject = "E2E data protection email",
            EmailFormat = EmailFormatEnum.Html,
            EmailStatus = EmailStatusEnum.Archived,
            EmailPriority = EmailPriorityEnum.Normal,
            EmailGUID = Guid.NewGuid(),
            EmailMailoutGuid = mailoutGuid,
            EmailCreated = DateTime.Now,
        };
        await emailProvider.SetAsync(sent);

        var configuration = (await emailConfigurationProvider.Get().TopN(1).GetEnumerableTypedResultAsync()).First();
        var hit = new EmailStatisticsHitsInfo
        {
            EmailStatisticsHitsEmailConfigurationID = configuration.EmailConfigurationID,
            EmailStatisticsHitsMailoutGUID = mailoutGuid,
            EmailStatisticsHitsType = EmailStatisticsHitsType.Open,
            EmailStatisticsHitsTime = DateTime.Now,
            EmailStatisticsHitsIsProcessed = true,
        };
        await hitsProvider.SetAsync(hit);

        return (sent.EmailID, mailoutGuid, hit.EmailStatisticsHitsID);
    }

    /// <summary>Runs every registered identity and data collector, as the Data protection app does.</summary>
    public string Collect(string email, string format)
    {
        var identities = IdentityCollectorRegister.Instance.CollectIdentities(Identifier(email));
        var results = PersonalDataCollectorRegister.Instance.CollectData(identities, format);

        return PersonalDataHelper.JoinPersonalData(results.Select(r => r.Text), format);
    }

    /// <summary>Runs every registered eraser, as the Right to be forgotten dialog does. All options on unless given.</summary>
    public void Erase(string email, IReadOnlyDictionary<string, bool>? options)
    {
        var identities = IdentityCollectorRegister.Instance.CollectIdentities(Identifier(email));
        var configuration = ErasureOptionKeys.All.ToDictionary(
            k => k,
            k => (object)(options is null || options.GetValueOrDefault(k)));

        PersonalDataEraserRegister.Instance.EraseData(identities, configuration);
    }

    public async Task<E2EDataProtectionState> GetState(E2EDataProtectionSeed seed)
    {
        var member = (await memberProvider.Get()
            .WhereEquals(nameof(MemberInfo.MemberID), seed.MemberID)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        var forms = formDataService.GetForms();
        var rows = seed.FormRows.Select(r =>
        {
            var form = forms.First(f => f.ClassName == r.ClassName);
            var item = BizFormItemProvider.GetItem(r.ItemID, r.ClassName);

            return new E2EFormRowState(
                r.FormName,
                item is not null,
                item is null ? null : form.Shape.AllEmailColumns.Select(c => item.GetStringValue(c, "")).FirstOrDefault(v => v.Length > 0),
                item is null || form.Shape.MemberIDColumn is null ? null : item.GetIntegerValue(form.Shape.MemberIDColumn, 0));
        }).ToList();

        var hit = (await hitsProvider.Get()
            .WhereEquals(nameof(EmailStatisticsHitsInfo.EmailStatisticsHitsID), seed.EmailStatisticsHitID)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        return new E2EDataProtectionState(
            MemberExists: member is not null,
            MemberEmail: member?.MemberEmail,
            MemberUserName: member?.MemberName,
            MemberFirstName: member?.GetStringValue("MemberFirstName", ""),
            MemberLastName: member?.GetStringValue("MemberLastName", ""),
            MemberBio: member?.GetStringValue("MemberBio", ""),
            MemberLinkedIn: member?.GetStringValue("MemberLinkedInIdentifier", ""),
            MemberModerationStatus: member?.GetStringValue("MemberModerationStatus", ""),
            MemberEnabled: member?.MemberEnabled,
            ExternalLogins: await externalLoginProvider.Get()
                .WhereEquals(nameof(MemberExternalLoginInfo.MemberExternalLoginMemberID), seed.MemberID)
                .GetCountAsync(),
            NotificationSettings: await notificationSettingsProvider.Get()
                .WhereEquals(nameof(DiscussionMemberNotificationSettingsInfo.DiscussionMemberNotificationSettingsMemberID), seed.MemberID)
                .GetCountAsync(),
            Badges: await badgeMemberProvider.Get()
                .WhereEquals(nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberId), seed.MemberID)
                .GetCountAsync(),
            // By email, not seeded IDs: Xperience merges same-email contacts in the background
            // (member registration sets an email, queuing a merge), so seeded IDs can disappear
            Contacts: await contactProvider.Get()
                .WhereEquals(nameof(ContactInfo.ContactEmail), seed.Email)
                .GetCountAsync(),
            ListMembershipExists: await contactGroupMemberProvider.Get()
                .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberID), seed.ListMembershipID)
                .GetCountAsync() > 0,
            FormRows: rows,
            SentEmailExists: await emailProvider.Get()
                .WhereEquals(nameof(EmailInfo.EmailID), seed.SentEmailID)
                .GetCountAsync() > 0,
            EmailStatisticsHitExists: hit is not null,
            EmailStatisticsHitMailoutGuid: hit?.EmailStatisticsHitsMailoutGUID);
    }

    /// <summary>Sets or (with a null policy) removes a form's retention policy. Returns the previous one so a spec can restore it.</summary>
    public async Task<Result<E2EFormRetentionPolicy>> SetFormRetentionPolicy(E2EFormRetentionPolicy request)
    {
        var form = (await formProvider.Get()
            .WhereEquals(nameof(BizFormInfo.FormName), request.FormName)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();
        if (form is null)
        {
            return Result.Failure<E2EFormRetentionPolicy>($"Form '{request.FormName}' was not found.");
        }

        if (request.Policy is not null && !FormRetentionPolicyTypes.All.Contains(request.Policy))
        {
            return Result.Failure<E2EFormRetentionPolicy>($"Unknown policy '{request.Policy}'. Use {string.Join(", ", FormRetentionPolicyTypes.All)} or null.");
        }

        var existing = (await policyProvider.Get()
            .WhereEquals(nameof(FormRetentionPolicyInfo.FormRetentionPolicyFormID), form.FormID)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        var previous = new E2EFormRetentionPolicy(
            form.FormName,
            existing?.FormRetentionPolicyType,
            existing?.FormRetentionPolicyRetentionDays ?? 0,
            existing?.FormRetentionPolicyGUID);

        if (request.Policy is null)
        {
            if (existing is not null)
            {
                await policyProvider.DeleteAsync(existing);
            }
        }
        else
        {
            existing ??= new FormRetentionPolicyInfo
            {
                // Restoring a removed policy reuses its GUID, so the CI file doesn't change
                FormRetentionPolicyGUID = request.PolicyGuid ?? Guid.NewGuid(),
                FormRetentionPolicyFormID = form.FormID,
            };
            existing.FormRetentionPolicyType = request.Policy;
            existing.FormRetentionPolicyRetentionDays = request.RetentionDays;
            await policyProvider.SetAsync(existing);
        }

        return previous;
    }

    /// <summary>Inserts a form row with a backdated <c>FormInserted</c>, for the form retention task.</summary>
    public Result<E2ESeededFormRow> SeedFormRow(E2ESeedFormRowRequest request)
    {
        var form = formDataService.GetForms()
            .FirstOrDefault(f => string.Equals(f.Form.FormName, request.FormName, StringComparison.OrdinalIgnoreCase));
        if (form is null)
        {
            return Result.Failure<E2ESeededFormRow>($"Form '{request.FormName}' was not found.");
        }

        var item = NewFormItem(form.ClassName, (request.Values ?? []).ToDictionary(kv => kv.Key, kv => (object)(kv.Value.ToString() ?? "")));
        item.Insert();

        item.SetValue("FormInserted", DateTime.Now.AddDays(-request.InsertedDaysAgo));
        item.Update();

        return new E2ESeededFormRow(form.Form.FormName, form.ClassName, item.ItemID);
    }

    /// <summary>A form row with the given values, and placeholders in the other required fields.</summary>
    private static BizFormItem NewFormItem(string className, IReadOnlyDictionary<string, object> values)
    {
        var item = BizFormItem.New(className);
        var formInfo = new CMS.FormEngine.FormInfo(DataClassInfoProvider.GetDataClassInfo(className).ClassFormDefinition);

        foreach (var field in formInfo.GetFields(visible: true, invisible: true, includeSystem: false, onlyPrimaryKeys: false, includeDummyFields: false))
        {
            if (values.TryGetValue(field.Name, out object? value))
            {
                item.SetValue(field.Name, value);
            }
            else if (!field.AllowEmpty)
            {
                item.SetValue(field.Name, field.DataType switch
                {
                    "integer" or "longinteger" or "decimal" or "double" => 0,
                    "boolean" => false,
                    "datetime" or "date" => DateTime.Now,
                    _ => "e2e",
                });
            }
        }

        return item;
    }

    public bool FormRowExists(string className, int itemID) =>
        BizFormItemProvider.GetItem(itemID, className) is not null;

    private static Dictionary<string, object> Identifier(string email) =>
        new() { [PersonalDataConstants.DATA_SUBJECT_IDENTIFIER_KEY] = email };
}

public record E2EDataProtectionSeedRequest(string? Prefix = null);

public record E2ESeededFormRow(string FormName, string ClassName, int ItemID);

public record E2EDataProtectionSeed(
    int MemberID,
    string UserName,
    string Email,
    string Password,
    IReadOnlyList<int> ContactIDs,
    IReadOnlyList<E2ESeededFormRow> FormRows,
    int ListMembershipID,
    int SentEmailID,
    Guid MailoutGuid,
    long EmailStatisticsHitID);

public record E2EFormRowState(string FormName, bool Exists, string? Email, int? MemberID);

public record E2EDataProtectionState(
    bool MemberExists,
    string? MemberEmail,
    string? MemberUserName,
    string? MemberFirstName,
    string? MemberLastName,
    string? MemberBio,
    string? MemberLinkedIn,
    string? MemberModerationStatus,
    bool? MemberEnabled,
    int ExternalLogins,
    int NotificationSettings,
    int Badges,
    int Contacts,
    bool ListMembershipExists,
    IReadOnlyList<E2EFormRowState> FormRows,
    bool SentEmailExists,
    bool EmailStatisticsHitExists,
    Guid? EmailStatisticsHitMailoutGuid);

public record E2EDataProtectionCollectRequest(string Email, string Format);

public record E2EDataProtectionCollectResponse(string Text);

/// <param name="Options">Erasure options by <see cref="ErasureOptionKeys"/> key. Omit for all on.</param>
public record E2EDataProtectionEraseRequest(string Email, IReadOnlyDictionary<string, bool>? Options = null);

/// <param name="Policy">History, ShortLived, or null to remove the policy (unclassified).</param>
/// <param name="PolicyGuid">GUID for a newly created policy row; pass back the one a previous call returned to restore it unchanged.</param>
public record E2EFormRetentionPolicy(string FormName, string? Policy, int RetentionDays = 0, Guid? PolicyGuid = null);

public record E2ESeedFormRowRequest(string FormName, int InsertedDaysAgo, Dictionary<string, object>? Values = null);

public record E2EFormRowExistsResponse(bool Exists);
