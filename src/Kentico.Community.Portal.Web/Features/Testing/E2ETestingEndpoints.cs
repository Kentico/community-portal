using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Modules.Membership;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace Kentico.Community.Portal.Web.Features.Testing;

public static class E2ETestingEndpoints
{
    public static RouteGroupBuilder MapE2ETestingTools(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/testing/e2e")
            .WithTags("E2E Testing Tools")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoints used by Playwright to prepare application state for deterministic E2E tests.");

        group.MapPost(
                "/member-enabled",
                async Task<Results<Ok<E2EMemberEnabledResponse>, NotFound<string>>> (
                    E2EMemberEnabledRequest request,
                    E2EMemberModerationService moderationService,
                    CancellationToken cancellationToken) =>
                {
                    var result = await moderationService.SetEnabledAsync(request.UserName, request.Enabled, cancellationToken);

                    return result is null
                        ? TypedResults.NotFound($"Member '{request.UserName}' was not found.")
                        : TypedResults.Ok(result);
                })
            .WithName("SetMemberEnabledForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint used by Playwright to enable or disable a member, like an administrator does.");

        group.MapPost(
                "/member-moderation",
                async Task<Results<Ok<E2EMemberModerationResponse>, NotFound<string>, BadRequest<string>>> (
                    E2EMemberModerationRequest request,
                    E2EMemberModerationService moderationService,
                    CancellationToken cancellationToken) =>
                {
                    if (!Enum.TryParse<ModerationStatuses>(request.ModerationStatus, ignoreCase: true, out var moderationStatus))
                    {
                        return TypedResults.BadRequest($"Unsupported moderation status '{request.ModerationStatus}'.");
                    }

                    var result = await moderationService.UpdateAsync(request.UserName, moderationStatus, cancellationToken);

                    return result is null
                        ? TypedResults.NotFound($"Member '{request.UserName}' was not found.")
                        : TypedResults.Ok(result);
                })
            .WithName("SetMemberModerationForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint used by Playwright to prepare member moderation state.");

        group.MapPost(
                "/member-role",
                async Task<Results<Ok<E2EMemberRoleResponse>, NotFound<string>, BadRequest<string>>> (
                    E2EMemberRoleRequest request,
                    E2EMemberRoleService roleService) =>
                {
                    var result = await roleService.SetAsync(request.UserName, request.RoleName, request.Granted);

                    if (result.IsSuccess)
                    {
                        return TypedResults.Ok(new E2EMemberRoleResponse(request.UserName, request.RoleName, request.Granted));
                    }

                    return result.Error == E2EMemberRoleService.MEMBER_NOT_FOUND
                        ? TypedResults.NotFound($"Member '{request.UserName}' was not found.")
                        : TypedResults.BadRequest(result.Error);
                })
            .WithName("SetMemberRoleForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint used by Playwright to grant or revoke one of the SystemMemberRoles for a member.");

        group.MapPrivacyTools();
        group.MapDataProtectionTools();

        return group;
    }

    /// <summary>
    /// Data protection and form retention tools. Collect and erase run the registered
    /// Xperience collectors and erasers, the same path as the admin Data protection app.
    /// </summary>
    private static void MapDataProtectionTools(this RouteGroupBuilder group)
    {
        var dp = group.MapGroup("/data-protection");

        dp.MapPost("/seed-member", async Task<Results<Ok<E2EDataProtectionSeed>, BadRequest<string>>> (
                E2EDataProtectionSeedRequest request,
                E2EDataProtectionTestingService service) =>
            {
                var result = await service.SeedMember(request);

                return result.IsSuccess ? TypedResults.Ok(result.Value) : TypedResults.BadRequest(result.Error);
            })
            .WithName("SeedDataProtectionMemberForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint that seeds a member with profile fields, an external login, discussion settings, a badge, form rows, two contacts with activities, an agreement, a newsletter subscription, and a sent email with an open hit.");

        dp.MapPost("/collect", (E2EDataProtectionCollectRequest request, E2EDataProtectionTestingService service) =>
                TypedResults.Ok(new E2EDataProtectionCollectResponse(service.Collect(request.Email, request.Format))))
            .WithName("CollectPersonalDataForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint that runs the registered identity and data collectors for an email. Format: 'human' or 'machine'.");

        dp.MapPost("/erase", (E2EDataProtectionEraseRequest request, E2EDataProtectionTestingService service) =>
            {
                service.Erase(request.Email, request.Options);

                return TypedResults.Ok();
            })
            .WithName("ErasePersonalDataForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint that runs the registered erasers for an email with the given options (all on when omitted).");

        dp.MapPost("/state", async (E2EDataProtectionSeed seed, E2EDataProtectionTestingService service) =>
                TypedResults.Ok(await service.GetState(seed)))
            .WithName("GetDataProtectionStateForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint returning what is left of a seeded data subject.");

        var retention = group.MapGroup("/form-retention");

        retention.MapPost("/policy", async Task<Results<Ok<E2EFormRetentionPolicy>, BadRequest<string>>> (
                E2EFormRetentionPolicy request,
                E2EDataProtectionTestingService service) =>
            {
                var result = await service.SetFormRetentionPolicy(request);

                return result.IsSuccess ? TypedResults.Ok(result.Value) : TypedResults.BadRequest(result.Error);
            })
            .WithName("SetFormRetentionPolicyForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint that sets (or with a null policy removes) a form's retention policy and returns the previous one.");

        retention.MapPost("/seed-row", Results<Ok<E2ESeededFormRow>, BadRequest<string>> (
                E2ESeedFormRowRequest request,
                E2EDataProtectionTestingService service) =>
            {
                var result = service.SeedFormRow(request);

                return result.IsSuccess ? TypedResults.Ok(result.Value) : TypedResults.BadRequest(result.Error);
            })
            .WithName("SeedFormRowForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint that inserts a form submission with a backdated FormInserted.");

        retention.MapGet("/row-exists", (string className, int itemID, E2EDataProtectionTestingService service) =>
                TypedResults.Ok(new E2EFormRowExistsResponse(service.FormRowExists(className, itemID))))
            .WithName("GetFormRowExistsForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint returning whether a form submission still exists.");
    }

    /// <summary>
    /// State readers and seeders for the consent and contact tracking specs. Readers query the
    /// database directly so a spec asserts on stored state, not on the services under test.
    /// </summary>
    private static void MapPrivacyTools(this RouteGroupBuilder group)
    {
        group.MapGet("/contact", async Task<Results<Ok<E2EContactsResponse>, BadRequest<string>>> (
                E2EPrivacyTestingService privacyService,
                CancellationToken cancellationToken,
                string? email = null,
                Guid? guid = null) =>
            {
                if (guid is Guid contactGuid)
                {
                    return TypedResults.Ok(await privacyService.GetContactsByGuid(contactGuid, cancellationToken));
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    return TypedResults.Ok(await privacyService.GetContactsByEmail(email, cancellationToken));
                }

                return TypedResults.BadRequest("Provide 'email' or 'guid'.");
            })
            .WithName("GetContactForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint returning the contacts matching an email (newest first) or a contact GUID, with their contact groups.");

        group.MapGet("/contacts/created-by", Results<Ok<E2ECorrelatedContactsResponse>, BadRequest<string>> (
                string correlation,
                E2EContactCorrelationStore correlationStore) =>
                string.IsNullOrWhiteSpace(correlation)
                    ? TypedResults.BadRequest("Provide 'correlation'.")
                    : TypedResults.Ok(new E2ECorrelatedContactsResponse(correlationStore.Get(correlation))))
            .WithName("GetContactsCreatedByCorrelationForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription($"Development/CI-only endpoint returning the contacts inserted during requests that sent the '{E2EContactCorrelation.HEADER_NAME}' header with this value.");

        group.MapGet("/consent-agreements", async Task<Results<Ok<IReadOnlyList<E2EConsentAgreement>>, BadRequest<string>>> (
                string email,
                E2EPrivacyTestingService privacyService,
                CancellationToken cancellationToken,
                string? consent = null) =>
                string.IsNullOrWhiteSpace(email)
                    ? TypedResults.BadRequest("Provide 'email'.")
                    : TypedResults.Ok(await privacyService.GetAgreementsByEmail(email, consent, cancellationToken)))
            .WithName("GetConsentAgreementsByEmailForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint returning every consent agreement row, oldest first, for all contacts with an email. Optionally filtered to one consent.");

        group.MapGet("/consent-agreements/by-contact", async Task<Ok<IReadOnlyList<E2EConsentAgreement>>> (
                Guid guid,
                E2EPrivacyTestingService privacyService,
                CancellationToken cancellationToken,
                string? consent = null) =>
                TypedResults.Ok(await privacyService.GetAgreementsByContact(guid, consent, cancellationToken)))
            .WithName("GetConsentAgreementsByContactForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint returning every consent agreement row, oldest first, for a contact GUID. Optionally filtered to one consent.");

        group.MapGet("/activities", async Task<Results<Ok<IReadOnlyList<E2EActivity>>, BadRequest<string>>> (
                string email,
                E2EPrivacyTestingService privacyService,
                CancellationToken cancellationToken,
                string? type = null) =>
                string.IsNullOrWhiteSpace(email)
                    ? TypedResults.BadRequest("Provide 'email'.")
                    : TypedResults.Ok(await privacyService.GetActivitiesByEmail(email, type, cancellationToken)))
            .WithName("GetActivitiesByEmailForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint returning the activities, oldest first, of all contacts with an email. Optionally filtered to one activity type code name.");

        group.MapGet("/recipient-list", async Task<Results<Ok<E2ERecipientListResponse>, NotFound<string>>> (
                string email,
                E2EPrivacyTestingService privacyService,
                CancellationToken cancellationToken,
                string list = E2EPrivacyTestingService.DEFAULT_RECIPIENT_LIST) =>
            {
                var result = await privacyService.GetRecipientListState(email, list, cancellationToken);

                return result is null
                    ? TypedResults.NotFound($"Recipient list '{list}' was not found.")
                    : TypedResults.Ok(result);
            })
            .WithName("GetRecipientListStateForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint returning whether an email is subscribed to a recipient list, and its subscription confirmation date.");

        group.MapGet("/consent", async Task<Results<Ok<E2EConsentResponse>, NotFound<string>>> (
                string name,
                E2ENewsletterTestingService newsletterService,
                CancellationToken cancellationToken) =>
            {
                var consent = await newsletterService.GetConsent(name, cancellationToken);

                return consent is null
                    ? TypedResults.NotFound($"Consent '{name}' was not found.")
                    : TypedResults.Ok(consent);
            })
            .WithName("GetConsentForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint returning a consent's short and full text and hash, so specs can compare rendered text with the admin text.");

        group.MapPost("/newsletter-unsubscribe-url", async Task<Results<Ok<E2EUnsubscribeUrlResponse>, BadRequest<string>>> (
                E2EUnsubscribeUrlRequest request,
                E2ENewsletterTestingService newsletterService,
                CancellationToken cancellationToken) =>
            {
                var result = await newsletterService.GetNewsletterUnsubscribeUrl(request.Email, cancellationToken);

                return result.IsSuccess
                    ? TypedResults.Ok(result.Value)
                    : TypedResults.BadRequest(result.Error);
            })
            .WithName("GetNewsletterUnsubscribeUrlForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint returning the signed unsubscribe link a newsletter email would carry for an address, generated by Xperience.");

        group.MapGet("/member", async Task<Results<Ok<E2EMemberResponse>, NotFound<string>>> (
                string userName,
                E2EPrivacyTestingService privacyService,
                CancellationToken cancellationToken) =>
            {
                var member = await privacyService.GetMember(userName, cancellationToken);

                return member is null
                    ? TypedResults.NotFound($"Member '{userName}' was not found.")
                    : TypedResults.Ok(member);
            })
            .WithName("GetMemberForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint returning a member's email, moderation status, and terms and newsletter opt-in fields (null until those fields exist).");

        group.MapPost("/run-task", async Task<Results<Ok<E2ERunTaskResponse>, NotFound<string>>> (
                E2ERunTaskRequest request,
                E2EScheduledTaskRunner taskRunner,
                CancellationToken cancellationToken) =>
            {
                var result = await taskRunner.Run(request.CodeName, cancellationToken);

                return result.IsSuccess
                    ? TypedResults.Ok(result.Value)
                    : TypedResults.NotFound(result.Error);
            })
            .WithName("RunScheduledTaskForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint that runs a scheduled task inline, by configuration code name or registered task identifier.");

        group.MapPost("/seed-contacts", async Task<Results<Ok<IReadOnlyList<E2ESeededContact>>, BadRequest<string>>> (
                E2ESeedContactsRequest request,
                E2EPrivacyTestingService privacyService,
                CancellationToken cancellationToken) =>
            {
                var result = await privacyService.SeedContacts(request, cancellationToken);

                return result.IsSuccess
                    ? TypedResults.Ok(result.Value)
                    : TypedResults.BadRequest(result.Error);
            })
            .WithName("SeedContactsForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint that seeds one contact of each kind (anonymous, email, subscriber, member, with agreement) with backdated creation and activity.");

        group.MapPost("/tag-manager-snippet", async Task<Results<Ok<E2ETagManagerSnippetResponse>, BadRequest<string>>> (
                E2ETagManagerSnippetRequest request,
                E2ETagManagerStub tagManagerStub,
                CancellationToken cancellationToken) =>
            {
                var result = await tagManagerStub.SetEnabled(request.Enabled, cancellationToken);

                return result.IsSuccess
                    ? TypedResults.Ok(result.Value)
                    : TypedResults.BadRequest(result.Error);
            })
            .WithName("SetTagManagerStubForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint that creates or removes a Tag Manager snippet recording dataLayer pushes into window.__gtmStub. Never serialized to CI.");

        group.MapPost("/read-only", (E2EReadOnlyModeRequest request, E2EReadOnlyModeSwitch readOnlySwitch) =>
                TypedResults.Ok(readOnlySwitch.SetEnabled(request.Enabled)))
            .WithName("SetReadOnlyModeForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint that switches read-only mode for the whole process. Use from serial specs only.");

        group.MapGet("/read-only", (E2EReadOnlyModeSwitch readOnlySwitch) => TypedResults.Ok(readOnlySwitch.Get()))
            .WithName("GetReadOnlyModeForE2E")
            .WithSummary("Tool for E2E testing only")
            .WithDescription("Development/CI-only endpoint returning whether read-only mode is on.");
    }
}

public record E2EMemberModerationRequest(string UserName, string ModerationStatus);

public record E2EMemberModerationResponse(string UserName, string ModerationStatus);

public class E2EMemberModerationService(IInfoProvider<MemberInfo> memberInfoProvider)
{
    private readonly IInfoProvider<MemberInfo> memberInfoProvider = memberInfoProvider;

    public async Task<E2EMemberModerationResponse?> UpdateAsync(
        string userName,
        ModerationStatuses moderationStatus,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        var members = await memberInfoProvider
            .Get()
            .WhereEquals(nameof(MemberInfo.MemberName), userName)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var member = members.FirstOrDefault();
        if (member is null)
        {
            return null;
        }

        member.MemberModerationStatus = moderationStatus.ToString();
        await memberInfoProvider.SetAsync(member);

        return new E2EMemberModerationResponse(member.MemberName, member.MemberModerationStatus);
    }

    /// <summary>Like an administrator toggling Enabled on the member Edit form: a member save, not Identity.</summary>
    public async Task<E2EMemberEnabledResponse?> SetEnabledAsync(string userName, bool enabled, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        var member = (await memberInfoProvider
            .Get()
            .WhereEquals(nameof(MemberInfo.MemberName), userName)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .FirstOrDefault();
        if (member is null)
        {
            return null;
        }

        member.MemberEnabled = enabled;
        await memberInfoProvider.SetAsync(member);

        return new E2EMemberEnabledResponse(member.MemberName, member.MemberEnabled);
    }
}

public record E2EMemberEnabledRequest(string UserName, bool Enabled);

public record E2EMemberEnabledResponse(string UserName, bool Enabled);

public record E2EMemberRoleRequest(string UserName, string RoleName, bool Granted);

public record E2EMemberRoleResponse(string UserName, string RoleName, bool Granted);

/// <summary>
/// Grants or revokes one of the <see cref="SystemMemberRoles" /> for a member, so Playwright specs can
/// deterministically set up the authenticated-with-role / authenticated-without-role scenarios that native
/// Member Role page security requires.
/// </summary>
public class E2EMemberRoleService(UserManager<Kentico.Community.Portal.Web.Membership.CommunityMember> userManager)
{
    public const string MEMBER_NOT_FOUND = "MEMBER_NOT_FOUND";

    private readonly UserManager<Kentico.Community.Portal.Web.Membership.CommunityMember> userManager = userManager;

    public async Task<Result> SetAsync(string userName, string roleName, bool granted)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        if (!SystemMemberRoles.ProtectedMemberRoles.Any(r => string.Equals(r.MemberRoleName, roleName, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure($"'{roleName}' is not a recognized SystemMemberRoles role.");
        }

        var member = await userManager.FindByNameAsync(userName);
        if (member is null)
        {
            return Result.Failure(MEMBER_NOT_FOUND);
        }

        bool isInRole = await userManager.IsInRoleAsync(member, roleName);

        var identityResult = (granted, isInRole) switch
        {
            (true, false) => await userManager.AddToRoleAsync(member, roleName),
            (false, true) => await userManager.RemoveFromRoleAsync(member, roleName),
            _ => IdentityResult.Success,
        };

        return identityResult.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join("; ", identityResult.Errors.Select(e => e.Description)));
    }
}
