using CMS.Base;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CookiePreferences;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.ComponentIcons;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(
    identifier: CookiePreferencesWidget.IDENTIFIER,
    viewComponentType: typeof(CookiePreferencesWidget),
    name: "Cookie preferences",
    propertiesType: typeof(CookiePreferencesWidgetProperties),
    Description = "Displays the visitor's cookie settings.",
    IconClass = KenticoIcons.COOKIE,
    AllowCache = false)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CookiePreferences;

/// <summary>
/// Cookie settings: Necessary (always on), Analytics and Website tracking toggles.
/// Copy: widget properties. The tracking text comes from the consent.
/// </summary>
public class CookiePreferencesWidget(CookieConsentManager cookieConsentManager, IReadOnlyModeProvider readOnlyProvider) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.CookiePreferences";

    private readonly CookieConsentManager cookieConsentManager = cookieConsentManager;
    private readonly IReadOnlyModeProvider readOnlyProvider = readOnlyProvider;

    public ViewViewComponentResult Invoke(ComponentViewModel<CookiePreferencesWidgetProperties> vm) =>
        View("~/Components/PageBuilder/Widgets/CookiePreferences/CookiePreferences.cshtml",
            new CookiePreferencesWidgetViewModel(
                vm.Properties,
                new CookieSettingsViewModel(
                    cookieConsentManager.GetChoice(),
                    HttpContext.User.Identity?.IsAuthenticated ?? false,
                    ShowSavedMessage: false,
                    readOnlyProvider.IsReadOnly)));
}

/// <summary>
/// Plain-text copy. The Website tracking row uses the consent's short and full text.
/// </summary>
public class CookiePreferencesWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = "Cookie settings";

    [TextAreaComponent(Label = "Intro", Order = 2)]
    public string Intro { get; set; } =
        "Choose which optional cookies and tracking you allow. Necessary cookies are always on because the site can't work without them. Your choice is saved in this browser. If you clear your cookies, sign out, or use another browser or device, we'll ask again.";

    [TextAreaComponent(Label = "Necessary cookies description", Order = 3)]
    public string NecessaryDescription { get; set; } =
        "Keep you signed in, protect against fraud, bots and spam (including Google reCAPTCHA on registration and forms with spam protection), and remember your cookie choices and dismissed site alerts.";

    [TextAreaComponent(Label = "Analytics description", Order = 4)]
    public string AnalyticsDescription { get; set; } =
        "Google Analytics measures how the site is used (pages viewed, time on site, device and browser type, approximate location from your IP address). We use it in aggregate to improve Kentico Community. Google processes this data on our behalf.";

    [TextAreaComponent(Label = "Member note", ExplanationText = "Shown to signed-in members only", Order = 5)]
    public string MemberNote { get; set; } =
        "Your choice doesn't affect your account. All member features work with necessary cookies only.";

    [TextAreaComponent(Label = "Sign-out note", Order = 6)]
    public string SignOutNote { get; set; } =
        "When you sign out, we reset this browser to necessary cookies only and forget your choice, so the next person using this browser starts fresh.";
}

public record CookiePreferencesWidgetViewModel(CookiePreferencesWidgetProperties Copy, CookieSettingsViewModel Settings);

/// <param name="Choice">The saved choice, or null when none was made</param>
/// <param name="IsMember">Signed in; shows the member note</param>
/// <param name="ShowSavedMessage">Response to a save</param>
/// <param name="IsReadOnly">Read-only mode: the tracking toggle is disabled</param>
public record CookieSettingsViewModel(ConsentCookie? Choice, bool IsMember, bool ShowSavedMessage, bool IsReadOnly)
{
    public const string READ_ONLY_NOTE = "Website tracking can't be turned on right now. We'll ask again later.";

    public bool Analytics => Choice?.Analytics ?? false;
    public bool Tracking => Choice?.Tracking ?? false;

    public string StatusText =>
        Choice is null
            ? "You haven't made a choice yet. Only necessary cookies are used."
            : $"Your current choice: {string.Join(" + ", Categories(Choice))}. Saved {Choice.SavedAt:yyyy-MM-dd}.";

    private static IEnumerable<string> Categories(ConsentCookie choice)
    {
        yield return "Necessary";

        if (choice.Analytics)
        {
            yield return "Analytics";
        }

        if (choice.Tracking)
        {
            yield return "Website tracking";
        }
    }
}

/// <summary>An optional category toggle. <paramref name="Oob"/>: htmx out-of-band update after a save.</summary>
public record CookieSettingsSwitch(string Id, string Name, bool Checked, bool Disabled, bool Oob = false);

public static class CookieSettingsSwitches
{
    public const string ANALYTICS_ID = "cookieSettingsAnalytics";
    public const string TRACKING_ID = "cookieSettingsTracking";

    public static CookieSettingsSwitch Analytics(CookieSettingsViewModel vm, bool oob = false) =>
        new(ANALYTICS_ID, "Analytics", vm.Analytics, Disabled: false, oob);

    public static CookieSettingsSwitch Tracking(CookieSettingsViewModel vm, bool oob = false) =>
        new(TRACKING_ID, "Tracking", vm.Tracking, Disabled: vm.IsReadOnly, oob);
}
