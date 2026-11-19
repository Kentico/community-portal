using CMS.Base;
using CMS.DataProtection;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

/// <summary>
/// Shows the banner until the visitor makes a choice. <see cref="CookieConsentMiddleware"/> decides
/// whether a valid choice exists (and forgets stale ones) earlier in the request.
/// </summary>
/// <remarks>
/// Copy is admin-editable: heading and message come from the <see cref="WebsiteCookieBannerSettingsContent"/>
/// item on the website settings. The message places the Website tracking consent short text at
/// <see cref="TRACKING_CONSENT_TOKEN"/> (appended after the message when the token is missing), so
/// the consent wording is edited only on the consent. In read-only mode a note replaces it.
/// </remarks>
public class CookieBannerViewComponent(
    IMediator mediator,
    IHttpContextAccessor contextAccessor,
    ConsentRetriever consentRetriever,
    IReadOnlyModeProvider readOnlyProvider) : ViewComponent
{
    public const string COOKIE_SETTINGS_PATH = "/cookies-policy#cookie-settings";

    /// <summary>
    /// Placeholder in the banner message for the Website tracking consent short text.
    /// </summary>
    public const string TRACKING_CONSENT_TOKEN = "{{WebsiteTrackingConsent}}";

    public const string READ_ONLY_NOTE = "Website tracking can't be turned on right now. You can still choose analytics, and we'll ask about tracking again later.";

    private readonly IMediator mediator = mediator;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly ConsentRetriever consentRetriever = consentRetriever;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var context = contextAccessor.HttpContext;
        bool noChoice = context?.Items.ContainsKey(CookieConsentMiddleware.SHOW_BANNER_ITEM) is true;
        // The settings widget is on this page
        bool isCookiePolicyPage = context?.Request.Path.Equals("/cookies-policy", StringComparison.OrdinalIgnoreCase) ?? false;

        if (!noChoice || isCookiePolicyPage)
        {
            return View("~/Features/DataCollection/CookieBanner.cshtml", CookieBannerViewModel.Hidden);
        }

        var settings = (await mediator.Send(new PortalWebsiteSettingsQuery())).CookieBannerSettings;

        return View("~/Features/DataCollection/CookieBanner.cshtml", new CookieBannerViewModel
        {
            Heading = settings.WebsiteCookieBannerSettingsContentHeading ?? "",
            MessageHTML = new HtmlString(ComposeMessage(settings.WebsiteCookieBannerSettingsContentMessageHTML ?? "", await GetTrackingHtml())),
        });
    }

    /// <summary>
    /// Puts the tracking text at <see cref="TRACKING_CONSENT_TOKEN"/>, or after the message when the
    /// admin removed the token.
    /// </summary>
    public static string ComposeMessage(string messageHtml, string trackingHtml) =>
        messageHtml.Contains(TRACKING_CONSENT_TOKEN, StringComparison.Ordinal)
            ? messageHtml.Replace(TRACKING_CONSENT_TOKEN, trackingHtml, StringComparison.Ordinal)
            : messageHtml + trackingHtml;

    private async Task<string> GetTrackingHtml()
    {
        if (readOnlyProvider.IsReadOnly)
        {
            return $"""<span class="xpcookiebanner__readonly" test-id="cookieBannerReadOnly">{HtmlEncoder.Default.Encode(READ_ONLY_NOTE)}</span>""";
        }

        var consent = await consentRetriever.GetConsent(ConsentCodeNames.WEBSITE_TRACKING);
        if (consent is null)
        {
            return "";
        }

        var text = await consent.GetConsentTextAsync(PortalWebSiteChannel.DEFAULT_LANGUAGE);

        return $"""<div class="xpcookiebanner__tracking" test-id="cookieBannerTracking">{text.ShortText}</div>""";
    }
}

public class CookieBannerViewModel
{
    public static CookieBannerViewModel Hidden { get; } = new() { HideBanner = true };

    public string Heading { get; init; } = "";
    public HtmlString MessageHTML { get; init; } = HtmlString.Empty;
    public bool HideBanner { get; init; }
}
