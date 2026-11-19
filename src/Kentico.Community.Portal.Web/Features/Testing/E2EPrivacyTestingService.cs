using CMS.Activities;
using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.EmailMarketing;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules.Membership;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Web.Infrastructure;

namespace Kentico.Community.Portal.Web.Features.Testing;

/// <summary>
/// Read and seed operations the privacy (consent, contact tracking, newsletter) E2E specs need.
/// Everything here reads the database directly instead of going through the services under test,
/// so a spec asserts on stored state rather than on the code it is checking.
/// </summary>
public class E2EPrivacyTestingService(
    IInfoProvider<ContactInfo> contactProvider,
    IInfoProvider<ContactGroupInfo> contactGroupProvider,
    IInfoProvider<ContactGroupMemberInfo> contactGroupMemberProvider,
    IInfoProvider<ConsentInfo> consentProvider,
    IInfoProvider<ConsentAgreementInfo> consentAgreementProvider,
    IInfoProvider<EmailSubscriptionConfirmationInfo> subscriptionConfirmationProvider,
    IInfoProvider<MemberInfo> memberProvider,
    IInfoProvider<ActivityInfo> activityProvider)
{
    /// <summary>
    /// The newsletter list, used when a seed request names no list.
    /// </summary>
    public const string DEFAULT_RECIPIENT_LIST = "KenticoCommunityNewsletter";

    /// <summary>
    /// Member custom fields for Terms acceptance and the newsletter opt-in. Read by name so this
    /// endpoint also works on a database without them.
    /// </summary>
    private const string MEMBER_TERMS_VERSION_FIELD = "MemberTermsAcceptedVersion";
    private const string MEMBER_TERMS_DATE_FIELD = "MemberTermsAcceptedDate";
    private const string MEMBER_NEWSLETTER_OPT_IN_FIELD = "MemberNewsletterOptInRequestedDate";
    private const string MEMBER_ALTERNATE_EMAIL_FIELD = "MemberAlternateEmail";
    private const string MEMBER_ALTERNATE_EMAIL_VERIFIED_FIELD = "MemberAlternateEmailVerifiedDate";
    private const string MEMBER_EMAIL_CONFIRMED_FIELD = "MemberEmailConfirmedDate";

    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;
    private readonly IInfoProvider<ContactGroupInfo> contactGroupProvider = contactGroupProvider;
    private readonly IInfoProvider<ContactGroupMemberInfo> contactGroupMemberProvider = contactGroupMemberProvider;
    private readonly IInfoProvider<ConsentInfo> consentProvider = consentProvider;
    private readonly IInfoProvider<ConsentAgreementInfo> consentAgreementProvider = consentAgreementProvider;
    private readonly IInfoProvider<EmailSubscriptionConfirmationInfo> subscriptionConfirmationProvider = subscriptionConfirmationProvider;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly IInfoProvider<ActivityInfo> activityProvider = activityProvider;

    public async Task<E2EContactsResponse> GetContactsByEmail(string email, CancellationToken cancellationToken)
    {
        var contacts = await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), email)
            .OrderByDescending(nameof(ContactInfo.ContactID))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return await ToContactsResponse([.. contacts], cancellationToken);
    }

    public async Task<E2EContactsResponse> GetContactsByGuid(Guid contactGuid, CancellationToken cancellationToken)
    {
        var contacts = await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactGUID), contactGuid)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return await ToContactsResponse([.. contacts], cancellationToken);
    }

    public async Task<IReadOnlyList<E2EConsentAgreement>> GetAgreementsByEmail(string email, string? consentName, CancellationToken cancellationToken)
    {
        var contacts = await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), email)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return await GetAgreements([.. contacts], consentName, cancellationToken);
    }

    public async Task<IReadOnlyList<E2EConsentAgreement>> GetAgreementsByContact(Guid contactGuid, string? consentName, CancellationToken cancellationToken)
    {
        var contacts = await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactGUID), contactGuid)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return await GetAgreements([.. contacts], consentName, cancellationToken);
    }

    /// <summary>
    /// Activities of every contact with the email, oldest first. Optionally filtered to one activity
    /// type code name (e.g. <c>memberregistration</c>, <c>emailclick</c>).
    /// </summary>
    public async Task<IReadOnlyList<E2EActivity>> GetActivitiesByEmail(string email, string? activityType, CancellationToken cancellationToken)
    {
        var contacts = (await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), email)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .ToDictionary(c => c.ContactID);

        if (contacts.Count == 0)
        {
            return [];
        }

        var query = activityProvider.Get()
            .WhereIn(nameof(ActivityInfo.ActivityContactID), contacts.Keys.ToList())
            .OrderBy(nameof(ActivityInfo.ActivityID));
        if (!string.IsNullOrWhiteSpace(activityType))
        {
            query = query.WhereEquals(nameof(ActivityInfo.ActivityType), activityType);
        }

        var activities = await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return [.. activities.Select(a => new E2EActivity(
            a.ActivityID,
            contacts[a.ActivityContactID].ContactGUID,
            a.ActivityType,
            a.ActivityTitle,
            a.ActivityValue,
            a.ActivityURL,
            a.ActivityCreated))];
    }

    /// <summary>
    /// Subscribed means what <see cref="Core.Membership.RecipientListManager" /> means: a contact list
    /// membership plus an approved latest subscription confirmation. Both halves are returned so a spec can
    /// tell "pending double opt-in" from "not subscribed".
    /// </summary>
    public async Task<E2ERecipientListResponse?> GetRecipientListState(string email, string listName, CancellationToken cancellationToken)
    {
        var list = (await contactGroupProvider.Get()
            .WhereEquals(nameof(ContactGroupInfo.ContactGroupName), listName)
            .WhereEquals(nameof(ContactGroupInfo.ContactGroupIsRecipientList), true)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .FirstOrDefault();

        if (list is null)
        {
            return null;
        }

        var contactIDs = (await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), email)
            .Column(nameof(ContactInfo.ContactID))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .Select(c => c.ContactID)
            .ToList();

        if (contactIDs.Count == 0)
        {
            return new E2ERecipientListResponse(email, listName, false, false, false, null, [], []);
        }

        bool isListMember = (await contactGroupMemberProvider.Get()
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberContactGroupID), list.ContactGroupID)
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
            .WhereIn(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contactIDs)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken)).Any();

        var confirmations = (await subscriptionConfirmationProvider.Get()
            .WhereEquals(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationRecipientListID), list.ContactGroupID)
            .WhereIn(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationContactID), contactIDs)
            .OrderBy(nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationDate), nameof(EmailSubscriptionConfirmationInfo.EmailSubscriptionConfirmationID))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .ToList();

        var confirmation = confirmations.LastOrDefault();
        bool isApproved = confirmation?.EmailSubscriptionConfirmationIsApproved ?? false;

        return new E2ERecipientListResponse(
            email,
            listName,
            Subscribed: isListMember && isApproved,
            IsListMember: isListMember,
            IsConfirmationApproved: isApproved,
            ConfirmationDate: confirmation?.EmailSubscriptionConfirmationDate,
            ContactIDs: contactIDs,
            Confirmations: [.. confirmations.Select(c => new E2ESubscriptionConfirmation(
                c.EmailSubscriptionConfirmationDate,
                c.EmailSubscriptionConfirmationIsApproved,
                c.EmailSubscriptionConfirmationContactID))]);
    }

    /// <summary>
    /// Terms and newsletter fields are read by column name and returned as null while the columns
    /// don't exist, so specs can call this on a database without them.
    /// </summary>
    public async Task<E2EMemberResponse?> GetMember(string userName, CancellationToken cancellationToken)
    {
        var member = (await memberProvider.Get()
            .WhereEquals(nameof(MemberInfo.MemberName), userName)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .FirstOrDefault();

        if (member is null)
        {
            return null;
        }

        return new E2EMemberResponse(
            member.MemberID,
            member.MemberName,
            member.MemberEmail,
            member.MemberEnabled,
            member.MemberModerationStatus,
            GetOptionalValue(member, MEMBER_TERMS_VERSION_FIELD) as string,
            GetOptionalValue(member, MEMBER_TERMS_DATE_FIELD) as DateTime?,
            GetOptionalValue(member, MEMBER_NEWSLETTER_OPT_IN_FIELD) as DateTime?,
            GetOptionalValue(member, MEMBER_ALTERNATE_EMAIL_FIELD) as string,
            GetOptionalValue(member, MEMBER_ALTERNATE_EMAIL_VERIFIED_FIELD) as DateTime?,
            GetOptionalValue(member, MEMBER_EMAIL_CONFIRMED_FIELD) as DateTime?);
    }

    /// <summary>
    /// Seeds one contact of each kind the retention tasks distinguish between.
    /// Every seeded email shares the prefix, so a spec can find its own rows among leftovers from
    /// earlier runs.
    /// </summary>
    public async Task<Result<IReadOnlyList<E2ESeededContact>>> SeedContacts(E2ESeedContactsRequest request, CancellationToken cancellationToken)
    {
        string prefix = string.IsNullOrWhiteSpace(request.Prefix)
            ? $"e2e-seed-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(10_000)}"
            : request.Prefix;
        var created = DateTime.Now.AddDays(-request.CreatedDaysAgo);
        DateTime? lastActivity = request.LastActivityDaysAgo is int activityDaysAgo
            ? DateTime.Now.AddDays(-activityDaysAgo)
            : null;

        string listName = request.RecipientListName ?? DEFAULT_RECIPIENT_LIST;
        var list = (await contactGroupProvider.Get()
            .WhereEquals(nameof(ContactGroupInfo.ContactGroupName), listName)
            .WhereEquals(nameof(ContactGroupInfo.ContactGroupIsRecipientList), true)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .FirstOrDefault();
        if (list is null)
        {
            return Result.Failure<IReadOnlyList<E2ESeededContact>>($"Recipient list '{listName}' was not found.");
        }

        var seeded = new List<E2ESeededContact>();

        var anonymous = await CreateContact(null, created, lastActivity);
        seeded.Add(ToSeeded(E2ESeededContactKind.Anonymous, anonymous));

        var emailOnly = await CreateContact($"{prefix}.email@kentico.local", created, lastActivity);
        seeded.Add(ToSeeded(E2ESeededContactKind.Email, emailOnly));

        var subscriber = await CreateContact($"{prefix}.subscriber@kentico.local", created, lastActivity);
        await Subscribe(subscriber, list, created);
        if (request.SubscriberWithoutAgreement)
        {
            await DeleteAgreements(subscriber, ConsentCodeNames.EMAIL_MARKETING, cancellationToken);
        }
        seeded.Add(ToSeeded(E2ESeededContactKind.Subscriber, subscriber));

        var memberContact = await CreateContact($"{prefix}.member@kentico.local", created, lastActivity);
        await CreateDisabledMember($"{prefix}-member", memberContact.ContactEmail, created);
        seeded.Add(ToSeeded(E2ESeededContactKind.Member, memberContact));

        foreach (var activity in request.Activities ?? [])
        {
            foreach (var contact in seeded)
            {
                activityProvider.Set(new ActivityInfo
                {
                    ActivityContactID = contact.ContactID,
                    ActivityType = activity.Type,
                    ActivityTitle = $"E2E seeded {activity.Type} {activity.DaysAgo} days ago",
                    ActivityURL = "/",
                    ActivityCreated = DateTime.Now.AddDays(-activity.DaysAgo),
                });
            }
        }

        return seeded;
    }

    /// <summary>
    /// Deletes the rows instead of revoking, so the contact looks like it never agreed.
    /// </summary>
    private async Task DeleteAgreements(ContactInfo contact, string consentName, CancellationToken cancellationToken)
    {
        if (await consentProvider.GetAsync(consentName, cancellationToken) is not ConsentInfo consent)
        {
            return;
        }

        var agreements = await consentAgreementProvider.Get()
            .WhereEquals(nameof(ConsentAgreementInfo.ConsentAgreementContactID), contact.ContactID)
            .WhereEquals(nameof(ConsentAgreementInfo.ConsentAgreementConsentID), consent.ConsentID)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        foreach (var agreement in agreements)
        {
            await consentAgreementProvider.DeleteAsync(agreement);
        }
    }

    private async Task<ContactInfo> CreateContact(string? email, DateTime created, DateTime? lastActivity)
    {
        var contact = new ContactInfo
        {
            ContactEmail = email ?? "",
            ContactLastName = email ?? $"e2e-anonymous-{Guid.NewGuid():N}",
        };
        await contactProvider.SetAsync(contact);

        // Set after insert because the insert stamps its own creation time
        contact.ContactCreated = created;
        await contactProvider.SetAsync(contact);

        if (lastActivity is DateTime activityDate)
        {
            var activity = new ActivityInfo
            {
                ActivityContactID = contact.ContactID,
                ActivityType = PredefinedActivityType.PAGE_VISIT,
                ActivityTitle = "E2E seeded page visit",
                ActivityURL = "/",
                ActivityCreated = activityDate,
            };
            activityProvider.Set(activity);
        }

        return contact;
    }

    private async Task Subscribe(ContactInfo contact, ContactGroupInfo list, DateTime confirmedAt)
    {
        await contactGroupMemberProvider.SetAsync(new ContactGroupMemberInfo
        {
            ContactGroupMemberContactGroupID = list.ContactGroupID,
            ContactGroupMemberType = ContactGroupMemberTypeEnum.Contact,
            ContactGroupMemberRelatedID = contact.ContactID,
            ContactGroupMemberFromManual = true,
            ContactGroupMemberFromCondition = false,
        });

        await subscriptionConfirmationProvider.SetAsync(new EmailSubscriptionConfirmationInfo
        {
            EmailSubscriptionConfirmationContactID = contact.ContactID,
            EmailSubscriptionConfirmationRecipientListID = list.ContactGroupID,
            EmailSubscriptionConfirmationIsApproved = true,
            EmailSubscriptionConfirmationDate = confirmedAt,
        });
    }

    /// <summary>
    /// Disabled so the seeded account can never sign in; only its email matters to the cleanup rules.
    /// </summary>
    private async Task CreateDisabledMember(string userName, string email, DateTime created) =>
        await memberProvider.SetAsync(new MemberInfo
        {
            MemberName = userName,
            MemberEmail = email,
            MemberEnabled = false,
            MemberIsExternal = false,
            MemberCreated = created,
            MemberSecurityStamp = Guid.NewGuid().ToString(),
        });

    private static E2ESeededContact ToSeeded(E2ESeededContactKind kind, ContactInfo contact) =>
        new(kind.ToString(), contact.ContactID, contact.ContactGUID, string.IsNullOrEmpty(contact.ContactEmail) ? null : contact.ContactEmail, contact.ContactCreated);

    private async Task<E2EContactsResponse> ToContactsResponse(IReadOnlyList<ContactInfo> contacts, CancellationToken cancellationToken)
    {
        if (contacts.Count == 0)
        {
            return new E2EContactsResponse(false, []);
        }

        var contactIDs = contacts.Select(c => c.ContactID).ToList();

        var memberships = await contactGroupMemberProvider.Get()
            .WhereEquals(nameof(ContactGroupMemberInfo.ContactGroupMemberType), ContactGroupMemberTypeEnum.Contact)
            .WhereIn(nameof(ContactGroupMemberInfo.ContactGroupMemberRelatedID), contactIDs)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var groupIDs = memberships.Select(m => m.ContactGroupMemberContactGroupID).Distinct().ToList();
        var groups = groupIDs.Count == 0
            ? []
            : (await contactGroupProvider.Get()
                .WhereIn(nameof(ContactGroupInfo.ContactGroupID), groupIDs)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
                .ToDictionary(g => g.ContactGroupID, g => g.ContactGroupName);

        return new E2EContactsResponse(true, [.. contacts.Select(c => new E2EContact(
            c.ContactID,
            c.ContactGUID,
            string.IsNullOrEmpty(c.ContactEmail) ? null : c.ContactEmail,
            c.ContactFirstName,
            c.ContactLastName,
            c.ContactCreated,
            [.. memberships
                .Where(m => m.ContactGroupMemberRelatedID == c.ContactID)
                .Select(m => groups.GetValueOrDefault(m.ContactGroupMemberContactGroupID))
                .OfType<string>()
                .Order()]))]);
    }

    /// <summary>
    /// Returns every agreement row, oldest first. Xperience writes a new row for each agree and
    /// revoke, so a contact's current state is its latest row for a consent.
    /// </summary>
    private async Task<IReadOnlyList<E2EConsentAgreement>> GetAgreements(IReadOnlyList<ContactInfo> contacts, string? consentName, CancellationToken cancellationToken)
    {
        if (contacts.Count == 0)
        {
            return [];
        }

        var consentQuery = consentProvider.Get();
        if (!string.IsNullOrWhiteSpace(consentName))
        {
            consentQuery = consentQuery.WhereEquals(nameof(ConsentInfo.ConsentName), consentName);
        }

        var consents = (await consentQuery.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .ToDictionary(c => c.ConsentID);

        if (consents.Count == 0)
        {
            return [];
        }

        var contactsByID = contacts.ToDictionary(c => c.ContactID);

        var agreements = await consentAgreementProvider.Get()
            .WhereIn(nameof(ConsentAgreementInfo.ConsentAgreementContactID), contactsByID.Keys.ToList())
            .WhereIn(nameof(ConsentAgreementInfo.ConsentAgreementConsentID), consents.Keys.ToList())
            .OrderBy(nameof(ConsentAgreementInfo.ConsentAgreementTime), nameof(ConsentAgreementInfo.ConsentAgreementID))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return [.. agreements.Select(a =>
        {
            var consent = consents[a.ConsentAgreementConsentID];
            var contact = contactsByID[a.ConsentAgreementContactID];

            return new E2EConsentAgreement(
                a.ConsentAgreementID,
                contact.ContactID,
                contact.ContactGUID,
                consent.ConsentName,
                a.ConsentAgreementRevoked,
                a.ConsentAgreementConsentHash,
                consent.ConsentHash,
                a.ConsentAgreementTime);
        })];
    }

    private static object? GetOptionalValue(BaseInfo info, string columnName) =>
        info.TryGetValue(columnName, out object? value) && value is not DBNull
            ? value
            : null;
}

public enum E2ESeededContactKind
{
    Anonymous,
    Email,
    Subscriber,
    Member,
}

public record E2EContact(
    int ContactID,
    Guid ContactGUID,
    string? Email,
    string FirstName,
    string LastName,
    DateTime Created,
    IReadOnlyList<string> ContactGroups);

public record E2EContactsResponse(bool Exists, IReadOnlyList<E2EContact> Contacts);

public record E2EConsentAgreement(
    int AgreementID,
    int ContactID,
    Guid ContactGUID,
    string ConsentName,
    bool Revoked,
    string AgreementConsentHash,
    string CurrentConsentHash,
    DateTime Time);

public record E2EActivity(
    int ActivityID,
    Guid ContactGUID,
    string ActivityType,
    string ActivityTitle,
    string ActivityValue,
    string ActivityURL,
    DateTime Created);

public record E2ERecipientListResponse(
    string Email,
    string ListName,
    bool Subscribed,
    bool IsListMember,
    bool IsConfirmationApproved,
    DateTime? ConfirmationDate,
    IReadOnlyList<int> ContactIDs,
    IReadOnlyList<E2ESubscriptionConfirmation> Confirmations);

/// <summary>
/// One subscription confirmation row. Xperience adds one per confirm and unsubscribe.
/// </summary>
public record E2ESubscriptionConfirmation(DateTime Date, bool IsApproved, int ContactID);

public record E2EMemberResponse(
    int MemberID,
    string UserName,
    string Email,
    bool Enabled,
    string? ModerationStatus,
    string? TermsAcceptedVersion,
    DateTime? TermsAcceptedDate,
    DateTime? NewsletterOptInRequestedDate,
    string? AlternateEmail,
    DateTime? AlternateEmailVerifiedDate,
    DateTime? EmailConfirmedDate);

/// <param name="Prefix">Shared by every seeded email and the member name. Generated when omitted.</param>
/// <param name="CreatedDaysAgo">Backdates <c>ContactCreated</c> (and the subscription and member dates).</param>
/// <param name="LastActivityDaysAgo">When set, each contact gets one page visit activity this many days ago.</param>
/// <param name="RecipientListName">List the <c>Subscriber</c> contact joins. Defaults to <see cref="E2EPrivacyTestingService.DEFAULT_RECIPIENT_LIST" />.</param>
/// <param name="Activities">Extra backdated activities every seeded contact gets, e.g. for the activity retention task.</param>
/// <param name="SubscriberWithoutAgreement">Deletes the Email marketing agreement the confirmation handler records for the
/// <c>Subscriber</c> contact, so it's subscribed without one (the drift the reconciliation task fixes).</param>
public record E2ESeedContactsRequest(
    string? Prefix = null,
    int CreatedDaysAgo = 0,
    int? LastActivityDaysAgo = null,
    string? RecipientListName = null,
    IReadOnlyList<E2ESeedActivity>? Activities = null,
    bool SubscriberWithoutAgreement = false);

/// <param name="Type">Activity type code name, e.g. <c>pagevisit</c> or <c>memberregistration</c>.</param>
/// <param name="DaysAgo">Backdates <c>ActivityCreated</c>.</param>
public record E2ESeedActivity(string Type, int DaysAgo);

public record E2ESeededContact(string Kind, int ContactID, Guid ContactGUID, string? Email, DateTime Created);
