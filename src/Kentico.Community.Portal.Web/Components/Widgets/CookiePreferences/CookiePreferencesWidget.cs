using CMS.Helpers;
using Kentico.Community.Portal.Web.Components.Widgets.CookiePreferences;
using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(
    identifier: CookiePreferencesWidget.Identifier,
    viewComponentType: typeof(CookiePreferencesWidget),
    name: "Cookie preferences",
    propertiesType: typeof(CookiePreferencesWidgetProperties),
    Description = "Displays a cookie preferences.",
    IconClass = "icon-cookie")]

namespace Kentico.Community.Portal.Web.Components.Widgets.CookiePreferences;

/// <summary>
/// Controller for form support widget.
/// </summary>
public class CookiePreferencesWidget : ViewComponent
{
    public const string Identifier = "CommunityPortal.CookiePreferences";
    private readonly ICookieAccessor cookies;

    public CookiePreferencesWidget(ICookieAccessor cookies) => this.cookies = cookies;

    public ViewViewComponentResult Invoke(CookiePreferencesWidgetProperties properties)
    {
        var vm = new CookiePreferencesWidgetViewModel
        {
            NecessaryHeader = properties.NecessaryHeader,
            NecessaryDescription = properties.NecessaryDescription,
            PreferenceHeader = properties.PreferenceHeader,
            PreferenceDescription = properties.PreferenceDescription,
            AnalyticalHeader = properties.AnalyticalHeader,
            AnalyticalDescription = properties.AnalyticalDescription,
            MarketingHeader = properties.MarketingHeader,
            MarketingDescription = properties.MarketingDescription,
            ButtonText = properties.ButtonText,
            CookieLevelSelected = ValidationHelper.GetInteger(cookies.Get(CookieNames.COOKIE_CONSENT_LEVEL), 1)
        };

        return View("~/Components/Widgets/CookiePreferences/CookiePreferences.cshtml", vm);
    }
}

public class CookiePreferencesWidgetProperties : IWidgetProperties
{
    [TextInputComponent(Label = "Necessary cookie header", Order = 1)]
    public string NecessaryHeader { get; set; } = "";

    [TextInputComponent(Label = "Necessary cookie description", Order = 2)]
    public string NecessaryDescription { get; set; } = "";

    [TextInputComponent(Label = "Preference cookie header", Order = 3)]
    public string PreferenceHeader { get; set; } = "";

    [TextInputComponent(Label = "Preference cookie description", Order = 4)]
    public string PreferenceDescription { get; set; } = "";

    [TextInputComponent(Label = "Analytical cookie header", Order = 5)]
    public string AnalyticalHeader { get; set; } = "";

    [TextInputComponent(Label = "Analytical cookie description", Order = 6)]
    public string AnalyticalDescription { get; set; } = "";

    [TextInputComponent(Label = "Marketing cookie header", Order = 7)]
    public string MarketingHeader { get; set; } = "";

    [TextInputComponent(Label = "Marketing cookie description", Order = 8)]
    public string MarketingDescription { get; set; } = "";

    [TextInputComponent(Label = "Button text", Order = 9)]
    public string ButtonText { get; set; } = "";
}

public class CookiePreferencesWidgetViewModel
{
    public string NecessaryHeader { get; set; } = "";
    public string NecessaryDescription { get; set; } = "";
    public string PreferenceHeader { get; set; } = "";
    public string PreferenceDescription { get; set; } = "";
    public string AnalyticalHeader { get; set; } = "";
    public string AnalyticalDescription { get; set; } = "";
    public string MarketingHeader { get; set; } = "";
    public string MarketingDescription { get; set; } = "";
    public string ButtonText { get; set; } = "";
    public int CookieLevelSelected { get; set; }
}
