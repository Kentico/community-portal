using System.Text.Json;
using Microsoft.AspNetCore.Html;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

/// <summary>
/// Google Consent Mode commands for GTM: <c>gtag('consent', 'default' | 'update', {...})</c>.
/// </summary>
/// <remarks>
/// <para>
/// GTM loads through Xperience Tag Manager only once the visitor allows Analytics
/// (<see cref="ConsentGatedCodeSnippetsService"/>). Consent Mode stays as a second control: GA4
/// tags fire only once <c>analytics_storage</c> is granted. The <c>ad_*</c> types are always denied.
/// Website tracking is Xperience-only and isn't a Consent Mode type.
/// </para>
/// <para>
/// Commands are pushed with <c>gtag()</c>, so each <c>dataLayer</c> entry is an <c>arguments</c>
/// object. GTM ignores plain objects or arrays as commands.
/// </para>
/// <para>
/// The <c>consent.default</c>/<c>consent.update</c> <c>dataLayer</c> events from before Consent
/// Mode are still pushed, in case existing GTM triggers or variables use them.
/// </para>
/// </remarks>
public static class ConsentModeScript
{
    /// <summary>Marks the page load script, which must run before any Tag Manager snippet.</summary>
    public const string MARKER_ATTRIBUTE = "data-consent-mode";

    public const string LEGACY_EVENT_DEFAULT = "consent.default";
    public const string LEGACY_EVENT_UPDATE = "consent.update";

    /// <summary>
    /// Page load: everything denied by default, then the saved choice when it grants analytics.
    /// </summary>
    /// <param name="choice">The saved choice, or null when none was made</param>
    public static IHtmlContent ForPageLoad(ConsentCookie? choice)
    {
        bool analytics = choice?.Analytics ?? false;

        var commands = new List<string>
        {
            Gtag("default", ConsentModeParameters(analytics: false)),
        };

        if (analytics)
        {
            commands.Add(Gtag("update", ConsentModeParameters(analytics: true)));
        }

        commands.Add(LegacyEvent(LEGACY_EVENT_DEFAULT, analytics, choice?.Tracking ?? false));

        return Script(commands, MARKER_ATTRIBUTE);
    }

    /// <summary>
    /// After a choice in the banner or cookie settings (rendered into the htmx response). When
    /// Analytics is allowed, Tag Manager then loads the snippets the gate held back, without a
    /// reload. Its script is absent outside website channel pages, hence the guard.
    /// </summary>
    public static IHtmlContent ForChoice(bool analytics, bool tracking) =>
        Script(
        [
            Gtag("update", ConsentModeParameters(analytics)),
            LegacyEvent(LEGACY_EVENT_UPDATE, analytics, tracking),
            .. analytics ? [LOAD_TAG_MANAGER_SNIPPETS] : Array.Empty<string>(),
        ]);

    /// <summary>
    /// Fetches the now allowed snippets from <c>/kentico.tagmanager/GetConsentedTags</c> and inserts the
    /// ones not on the page yet.
    /// </summary>
    public const string LOAD_TAG_MANAGER_SNIPPETS =
        "window.xperience?.tagManager?.updateCodeSnippets()?.catch(function () { });";

    public static IReadOnlyDictionary<string, string> ConsentModeParameters(bool analytics) =>
        new Dictionary<string, string>
        {
            ["analytics_storage"] = Grant(analytics),
            ["ad_storage"] = Grant(false),
            ["ad_user_data"] = Grant(false),
            ["ad_personalization"] = Grant(false),
        };

    private static string Grant(bool granted) => granted ? "granted" : "denied";

    private static string Gtag(string command, IReadOnlyDictionary<string, string> parameters) =>
        $"window.gtag('consent', '{command}', {JsonSerializer.Serialize(parameters)});";

    private static string LegacyEvent(string eventName, bool analytics, bool tracking)
    {
        var entry = new Dictionary<string, object>
        {
            ["event"] = eventName,
            ["consent"] = new Dictionary<string, string>
            {
                ["functionality_storage"] = Grant(true),
                ["personalization_storage"] = Grant(tracking),
                ["analytics_storage"] = Grant(analytics),
                ["ad_storage"] = Grant(false),
            },
        };

        return $"window.dataLayer.push({JsonSerializer.Serialize(entry)});";
    }

    /// <summary>
    /// A classic (not module) script, so it runs in document order, before later head scripts.
    /// </summary>
    private static HtmlString Script(IEnumerable<string> commands, string? marker = null) =>
        new($$"""
            <script{{(marker is null ? "" : " " + marker)}}>
            window.dataLayer = window.dataLayer || [];
            window.gtag = window.gtag || function () { window.dataLayer.push(arguments); };
            {{string.Join("\n", commands)}}
            </script>
            """);
}
