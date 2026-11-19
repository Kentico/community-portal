using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Helpers;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Identity;
using CookieLevel = Kentico.Web.Mvc.CookieLevel;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

/// <summary>
/// Applies a visitor's cookie choice: the Xperience cookie level, the Website tracking consent
/// agreement and the <see cref="CookieNames.CONSENT"/> cookie.
/// </summary>
/// <remarks>
/// Necessary cookies are always on (<see cref="CookieLevel.Essential"/>). Analytics is recorded in
/// the cookie only. Website tracking raises the level to <see cref="CookieLevel.All"/> and is backed
/// by a <see cref="ConsentCodeNames.WEBSITE_TRACKING"/> agreement on the visitor's contact.
/// </remarks>
public class CookieConsentManager(
    ConsentRetriever consentRetriever,
    IConsentAgreementService consentAgreementService,
    ICurrentCookieLevelProvider cookieLevelProvider,
    ICookieAccessor cookies,
    IHttpContextAccessor contextAccessor,
    MemberContactManager memberContactManager,
    UserManager<CommunityMember> userManager,
    IReadOnlyModeProvider readOnlyProvider,
    IInfoProvider<ContactInfo> contactProvider,
    ICurrentContactAccessor currentContact,
    TimeProvider clock,
    ILogger<CookieConsentManager> logger)
{
    private static readonly TimeSpan consentCookieLifetime = TimeSpan.FromDays(365);

    private readonly ConsentRetriever consentRetriever = consentRetriever;
    private readonly IConsentAgreementService consentAgreementService = consentAgreementService;
    private readonly ICurrentCookieLevelProvider cookieLevelProvider = cookieLevelProvider;
    private readonly ICookieAccessor cookies = cookies;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly MemberContactManager memberContactManager = memberContactManager;
    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;
    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;
    private readonly ICurrentContactAccessor currentContact = currentContact;
    private readonly TimeProvider clock = clock;
    private readonly ILogger<CookieConsentManager> logger = logger;

    /// <summary>
    /// The visitor's saved choice, or null when none was made (or it predates v2).
    /// </summary>
    public ConsentCookie? GetChoice() =>
        contextAccessor.HttpContext?.Items.ContainsKey(CookieConsentMiddleware.SHOW_BANNER_ITEM) is true
            ? null // Forgotten by the middleware this request; the request cookie is stale
            : ConsentCookie.Parse(cookies.Get(CookieNames.CONSENT));

    /// <summary>
    /// Applies and saves a cookie choice. Read-only mode: see <see cref="ApplyReadOnly"/>.
    /// </summary>
    /// <returns>The saved choice</returns>
    public async Task<ConsentCookie> Apply(bool analytics, bool tracking)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return await ApplyReadOnly(analytics, tracking);
        }

        if (tracking)
        {
            await StartTracking();
        }
        else
        {
            await StopTracking();
        }

        return SaveChoice(new ConsentCookie(analytics, tracking, clock.GetUtcNow()));
    }

    /// <summary>
    /// Saves the analytics choice and keeps the current tracking choice. For the cookie settings
    /// page in read-only mode, where the tracking switch is disabled and so isn't posted.
    /// </summary>
    /// <returns>The saved choice</returns>
    public Task<ConsentCookie> ApplyAnalyticsOnly(bool analytics)
    {
        var current = GetChoice();

        // Provisional: tracking was asked for. Pending revoke: tracking was withdrawn (Tracking is false).
        bool tracking = current is { Tracking: true } or { Provisional: true };

        return Apply(analytics, tracking);
    }

    /// <summary>
    /// Read-only mode: no agreement can be recorded or revoked.
    /// <list type="bullet">
    /// <item>Tracking, with an agreement already active in this browser: tracking stays on. Nothing
    /// needs writing.</item>
    /// <item>Tracking, otherwise: saved as <see cref="ConsentCookie.Provisional"/> at Essential, so the
    /// visitor is asked again after read-only mode ends. An earlier pending revoke is dropped, so
    /// the agreement isn't revoked later.</item>
    /// <item>No tracking: Essential. An active agreement is saved as a
    /// <see cref="ConsentCookie.PendingRevoke"/> that <see cref="CompletePendingRevoke"/> applies
    /// later; an earlier pending revoke is kept. Otherwise the choice is final.</item>
    /// </list>
    /// </summary>
    private async Task<ConsentCookie> ApplyReadOnly(bool analytics, bool tracking)
    {
        // Read before any level change, which removes the contact cookie
        var agreedContact = await GetAgreedTrackingContact();
        var now = clock.GetUtcNow();

        if (tracking && agreedContact is not null)
        {
            return SaveChoice(new ConsentCookie(analytics, Tracking: true, now));
        }

        cookieLevelProvider.SetCurrentCookieLevel(CookieLevel.Essential.Level);

        if (tracking)
        {
            return SaveChoice(new ConsentCookie(analytics, Tracking: false, now, Provisional: true));
        }

        // The earlier withdrawal's contact cookie is gone by now, so its GUID comes from the cookie
        var pendingRevoke = agreedContact?.ContactGUID
            ?? ConsentCookie.Parse(cookies.Get(CookieNames.CONSENT))?.PendingRevoke;

        return SaveChoice(new ConsentCookie(analytics, Tracking: false, now, PendingRevoke: pendingRevoke));
    }

    /// <summary>
    /// Restores tracking after sign-in when the member's contact already has the Website tracking
    /// agreement, then syncs the member to their contact. Otherwise the browser's
    /// current choice is left alone.
    /// </summary>
    public async Task OnMemberSignedIn(CommunityMember member)
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return;
        }

        var consent = await GetWebsiteTrackingConsent();
        var memberContact = await memberContactManager.GetContactByEmail(member.Email ?? "");

        if (consent is not null
            && memberContact is not null
            && consentAgreementService.IsAgreed(memberContact, consent))
        {
            cookieLevelProvider.SetCurrentCookieLevel(CookieLevel.All.Level);
            SaveChoice(new ConsentCookie(GetChoice()?.Analytics ?? false, true, clock.GetUtcNow()));
        }

        _ = await memberContactManager.SetMemberAsCurrentContact(member);
    }

    /// <summary>
    /// Sign-out: resets this browser to necessary cookies only and forgets the choice, so the
    /// next person using it (e.g. a shared computer) starts fresh. Creates no contact.
    /// </summary>
    public void ForgetVisitor()
    {
        cookieLevelProvider.SetCurrentCookieLevel(CookieLevel.Essential.Level);
        cookies.Remove(CookieNames.CONSENT);
        RemoveHostAnalyticsCookies();
    }

    /// <summary>
    /// Deletes Google Analytics cookies set on the portal host only. <c>.kentico.com</c> cookies
    /// belong to kentico.com's own consent banner and are never touched.
    /// </summary>
    public void RemoveHostAnalyticsCookies()
    {
        if (contextAccessor.HttpContext is not { } context)
        {
            return;
        }

        foreach (string name in context.Request.Cookies.Keys.Where(IsAnalyticsCookie))
        {
            // No Domain: matches only the host-only cookie
            context.Response.Cookies.Delete(name, new CookieOptions { Path = "/" });
        }

        static bool IsAnalyticsCookie(string name) =>
            name == "_ga" || name.StartsWith("_ga_", StringComparison.Ordinal);
    }

    private async Task StartTracking()
    {
        cookieLevelProvider.SetCurrentCookieLevel(CookieLevel.All.Level);

        // Members agree on their own contact, so the agreement follows them to other browsers
        ContactInfo? contact = null;
        if (contextAccessor.HttpContext?.User is { Identity.IsAuthenticated: true } user
            && await userManager.GetUserAsync(user) is { } member)
        {
            contact = await memberContactManager.SetMemberAsCurrentContact(member);
        }

        var consent = await GetWebsiteTrackingConsent();
        contact ??= currentContact.GetCurrentContact();

        if (consent is null || contact is null)
        {
            return;
        }

        if (!consentAgreementService.IsAgreed(contact, consent))
        {
            _ = consentAgreementService.Agree(contact, consent);
        }
    }

    private async Task StopTracking()
    {
        // Read before the level change, which removes the contact cookie
        if (await GetAgreedTrackingContact() is { } contact
            && await GetWebsiteTrackingConsent() is { } consent)
        {
            consentAgreementService.Revoke(contact, consent);
        }

        cookieLevelProvider.SetCurrentCookieLevel(CookieLevel.Essential.Level);
    }

    /// <summary>
    /// The current contact when it has the Website tracking agreement. Below <see cref="CookieLevel.All"/>
    /// there's no contact. Creates no contact.
    /// </summary>
    private async Task<ContactInfo?> GetAgreedTrackingContact() =>
        cookieLevelProvider.GetCurrentCookieLevel() >= CookieLevel.All.Level
            && currentContact.GetCurrentContact(createAnonymous: false) is { } contact
            && await GetWebsiteTrackingConsent() is { } consent
            && consentAgreementService.IsAgreed(contact, consent)
                ? contact
                : null;

    /// <summary>
    /// After read-only mode: revokes the Website tracking agreement a read-only withdrawal couldn't,
    /// and saves the choice as final (tracking off).
    /// </summary>
    /// <returns>The final choice</returns>
    public async Task<ConsentCookie> CompletePendingRevoke(ConsentCookie choice)
    {
        if (choice.PendingRevoke is not Guid contactGuid || readOnlyProvider.IsReadOnly)
        {
            return choice;
        }

        var contact = (await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactGUID), contactGuid)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (contact is not null
            && await GetWebsiteTrackingConsent() is { } consent
            && consentAgreementService.IsAgreed(contact, consent))
        {
            consentAgreementService.Revoke(contact, consent);
        }

        var final = choice with { Tracking = false, Provisional = false, PendingRevoke = null };
        SaveChoice(final);

        return final;
    }

    private ConsentCookie SaveChoice(ConsentCookie choice)
    {
        cookies.Set(CookieNames.CONSENT, choice.Format(), new CookieOptions
        {
            Expires = choice.SavedAt.Add(consentCookieLifetime),
            HttpOnly = true,
            Secure = true,
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
            IsEssential = true,
        });

        return choice;
    }

    private async Task<ConsentInfo?> GetWebsiteTrackingConsent()
    {
        var consent = await consentRetriever.GetConsent(ConsentCodeNames.WEBSITE_TRACKING);

        if (consent is null)
        {
            logger.LogError(new EventId(0, "CONSENT_MISSING"), "Consent {ConsentName} does not exist. Tracking choices can't be recorded.", ConsentCodeNames.WEBSITE_TRACKING);
        }

        return consent;
    }
}
