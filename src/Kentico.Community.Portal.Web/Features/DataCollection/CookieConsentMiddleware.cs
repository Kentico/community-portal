using CMS.Base;
using CMS.DataProtection;
using CMS.Helpers;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using CookieLevel = Kentico.Web.Mvc.CookieLevel;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

/// <summary>
/// Reconciles the visitor's cookie level with their saved choice before the page renders, so the
/// layout's tracking decisions (activity script, contact access) see the corrected level.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Pre-v2 consent cookies are deleted, so those visitors are asked again.</item>
/// <item>No valid choice, or Necessary only: the level drops to Essential if it's higher.</item>
/// <item>Provisional choice (tracking asked for in read-only mode): kept while read-only lasts;
/// afterwards forgotten, so the banner asks again. The level stays Essential either way.</item>
/// <item>Pending revoke (tracking withdrawn in read-only mode): after read-only, the agreement is
/// revoked and the choice saved as final (no banner).</item>
/// <item>Tracking chosen but no longer backed by an agreement (revoked in admin or another
/// browser), or the level was lost: the choice is forgotten, so the banner asks again.
/// Skipped in read-only mode, where agreements can't be recorded.</item>
/// <item>An unexpected failure is logged and the request continues at Essential with the banner
/// shown (the saved cookie is kept, so the next request can recover it).</item>
/// </list>
/// Sets <see cref="SHOW_BANNER_ITEM"/> when the visitor has no valid choice.
/// <para>
/// The agreement check uses <see cref="IConsentAgreementService.IsAgreed"/>, which Xperience
/// caches per contact and consent (cleared on agree/revoke), so no extra caching is needed here.
/// </para>
/// </remarks>
public class CookieConsentMiddleware(RequestDelegate next, ILogger<CookieConsentMiddleware> logger)
{
    public const string SHOW_BANNER_ITEM = "kcp.cookieconsent.showBanner";

    /// <summary>
    /// Requests that render no page and log no activities. Static files never get here
    /// (<c>UseStaticFiles</c> runs first).
    /// </summary>
    /// <remarks>
    /// Not skipped: <c>/cookies/</c> (a pending revoke must complete before a new choice overwrites
    /// the cookie), <c>/error/</c> (renders the layout) and htmx requests (some render full pages
    /// or log activities).
    /// </remarks>
    private static readonly string[] skippedPathPrefixes =
    [
        "/admin",
        "/kentico.",
        "/testing/",
        "/cmsctx",
        "/api/",
        // Content item assets and media library files
        "/getcontentasset/",
        "/getmedia/",
        // Avatar images
        "/member/avatar/",
        // Alert dismissal: sets an Essential cookie, returns no content
        "/notification/",
        "/robots.txt",
        "/sitemap.xml",
        "/llms.txt",
    ];

    private readonly RequestDelegate next = next;
    private readonly ILogger<CookieConsentMiddleware> logger = logger;

    public async Task InvokeAsync(
        HttpContext context,
        ICookieAccessor cookies,
        ICurrentCookieLevelProvider cookieLevelProvider,
        IConsentAgreementService consentAgreementService,
        ConsentRetriever consentRetriever,
        IReadOnlyModeProvider readOnlyProvider,
        CookieConsentManager cookieConsentManager,
        ICurrentContactAccessor currentContact)
    {
        if (!IsSkippedPath(context))
        {
            try
            {
                if (!IsPreviewOrEditMode(context))
                {
                    await Reconcile(context, cookies, cookieLevelProvider, consentAgreementService, consentRetriever, readOnlyProvider, cookieConsentManager, currentContact);
                }
            }
            catch (Exception ex) when (!context.RequestAborted.IsCancellationRequested)
            {
                FallBackToEssential(context, cookieLevelProvider, ex);
            }
        }

        await next(context);
    }

    private static async Task Reconcile(
        HttpContext context,
        ICookieAccessor cookies,
        ICurrentCookieLevelProvider cookieLevelProvider,
        IConsentAgreementService consentAgreementService,
        ConsentRetriever consentRetriever,
        IReadOnlyModeProvider readOnlyProvider,
        CookieConsentManager cookieConsentManager,
        ICurrentContactAccessor currentContact)
    {
        foreach (string name in CookieNames.LEGACY_CONSENT.Where(context.Request.Cookies.ContainsKey))
        {
            cookies.Remove(name);
        }

        string? rawChoice = cookies.Get(CookieNames.CONSENT);
        var choice = ConsentCookie.Parse(rawChoice);
        int level = cookieLevelProvider.GetCurrentCookieLevel();

        if (choice is { PendingRevoke: not null } && !readOnlyProvider.IsReadOnly)
        {
            choice = await cookieConsentManager.CompletePendingRevoke(choice);
        }

        if (choice is { Provisional: true } && !readOnlyProvider.IsReadOnly)
        {
            choice = null;
        }

        if (choice is null)
        {
            if (rawChoice is not null)
            {
                cookies.Remove(CookieNames.CONSENT);
            }

            LowerToEssential(cookieLevelProvider, level);
            context.Items[SHOW_BANNER_ITEM] = true;

            return;
        }

        if (!choice.Tracking)
        {
            LowerToEssential(cookieLevelProvider, level);

            return;
        }

        if (readOnlyProvider.IsReadOnly)
        {
            return;
        }

        bool stillAgreed = level >= CookieLevel.All.Level
            && await HasTrackingAgreement(consentAgreementService, consentRetriever, currentContact);

        if (!stillAgreed)
        {
            cookies.Remove(CookieNames.CONSENT);
            LowerToEssential(cookieLevelProvider, level);
            context.Items[SHOW_BANNER_ITEM] = true;
        }
    }

    private static async Task<bool> HasTrackingAgreement(
        IConsentAgreementService consentAgreementService,
        ConsentRetriever consentRetriever,
        ICurrentContactAccessor currentContact)
    {
        var consent = await consentRetriever.GetConsent(ConsentCodeNames.WEBSITE_TRACKING);

        // A missing consent is logged when a choice is saved. Don't reset every visitor over it.
        if (consent is null)
        {
            return true;
        }

        var contact = currentContact.GetCurrentContact(createAnonymous: false);

        return contact is not null && consentAgreementService.IsAgreed(contact, consent);
    }

    /// <summary>
    /// The safest state when reconciliation fails: no tracking this request and the banner shown.
    /// The consent cookie is kept, so a transient failure doesn't erase the visitor's choice.
    /// </summary>
    private void FallBackToEssential(HttpContext context, ICurrentCookieLevelProvider cookieLevelProvider, Exception ex)
    {
        logger.LogError(new EventId(0, "COOKIE_CONSENT_RECONCILE_FAILED"), ex, "Cookie consent reconciliation failed for {Path}. Falling back to Essential.", context.Request.Path.Value);

        context.Items[SHOW_BANNER_ITEM] = true;

        try
        {
            LowerToEssential(cookieLevelProvider, cookieLevelProvider.GetCurrentCookieLevel());
        }
        catch (Exception fallbackEx)
        {
            logger.LogError(new EventId(0, "COOKIE_CONSENT_FALLBACK_FAILED"), fallbackEx, "Cookie consent fallback couldn't lower the cookie level for {Path}.", context.Request.Path.Value);
        }
    }

    /// <summary>
    /// Lowers tracking levels (Visitor and up) only, so Editor-level admin sessions keep working.
    /// </summary>
    private static void LowerToEssential(ICurrentCookieLevelProvider cookieLevelProvider, int level)
    {
        if (level >= CookieLevel.Visitor.Level)
        {
            cookieLevelProvider.SetCurrentCookieLevel(CookieLevel.Essential.Level);
        }
    }

    private static bool IsSkippedPath(HttpContext context)
    {
        string path = context.Request.Path.Value ?? "";

        return skippedPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPreviewOrEditMode(HttpContext context) =>
        context.Kentico().Preview().Enabled
        || context.Kentico().PageBuilder().EditMode;
}
