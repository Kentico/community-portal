using System.ComponentModel;
using CMS.DataEngine;
using CMS.Websites.Routing;
using EnumsNET;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.FormBuilder;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Forms.Web.Mvc;
using Kentico.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http.Extensions;

[assembly: RegisterFormComponent(
    "DancingGoat.HiddenInput",
    typeof(HiddenInputComponent),
    "Hidden input",
    IsAvailableInFormBuilderEditor = true,
    IconClass = KenticoIcons.MASK,
    ViewName = "~/Components/FormBuilder/HiddenInput/HiddenInput.cshtml")]

namespace Kentico.Community.Portal.Web.Components.FormBuilder;

public class HiddenInputComponent(
    IFormBuilderContext formBuilderContext,
    IWebPageDataContextRetriever contextRetriever,
    IWebsiteChannelContext websiteChannelContext,
    IHttpContextAccessor httpContextAccessor,
    ICookieAccessor cookieAccessor) : FormComponent<HiddenInputProperties, string?>, IHiddenInputComponent
{
    public const string IDENTIFIER = "CommunityPortal.FormComponent.HiddenInput";

    private readonly IFormBuilderContext formBuilderContext = formBuilderContext;
    private readonly IWebPageDataContextRetriever contextRetriever = contextRetriever;
    private readonly IWebsiteChannelContext websiteChannelContext = websiteChannelContext;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly ICookieAccessor cookieAccessor = cookieAccessor;

    [BindableProperty]
    public string? Value { get; set; } = "";

    public FormBuilderMode FormBuilderMode => formBuilderContext.Mode;

    public override string GetValue() =>
        Properties.HiddenInputValueSourceParsed switch
        {
            /*
             * We explicitly don't expose cookie values into the View which could be a security vulnerability
             * so this value is accessed server-side.
             */
            HiddenInputValueSources.CookieValue => GetCookieValue(Properties.CookieName),
            HiddenInputValueSources.DefaultValue
            or HiddenInputValueSources.QueryStringValue
            or HiddenInputValueSources.WebPageURL
            or HiddenInputValueSources.WebPageID
            or HiddenInputValueSources.ChannelID
            or _ => Value ?? "",
        };

    public override void SetValue(string? value) => Value = value;

    public int GetWebPageID() =>
        contextRetriever.TryRetrieve(out var data)
            ? data.WebPage.WebPageItemID
            : 0;

    public int GetChannelID() =>
        websiteChannelContext.WebsiteChannelID;

    public string GetCookieValue(string cookieName) =>
        cookieAccessor.Get(cookieName) ?? "";

    public string GetQueryStringValue() =>
        httpContextAccessor.HttpContext is { } ctx
            ? ctx.Request.Query[Properties.QueryStringParameterName].ToString()
            : "";

    public string GetRequestURL() =>
        httpContextAccessor.HttpContext is { } ctx
            ? ctx.Request.GetDisplayUrl()
            : "";

    public HtmlString AdminDescription()
    {
        string sourceDisplayName = Enums
            .GetMembers<HiddenInputValueSources>(EnumMemberSelection.All)
            .Where(e => string.Equals(e.Value.ToString(), Properties.HiddenInputValueSource, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Attributes.OfType<DescriptionAttribute>().FirstOrDefault()?.Description ?? e.Name)
            .FirstOrDefault() ?? "";

        return new HtmlString(Properties.HiddenInputValueSourceParsed switch
        {
            HiddenInputValueSources.CookieValue => $"<em>{sourceDisplayName}</em> for cookie <strong>{Properties.CookieName}<strong>",
            HiddenInputValueSources.QueryStringValue => $"<em>{sourceDisplayName}</em> for query param <strong>{Properties.QueryStringParameterName}</strong>",
            HiddenInputValueSources.DefaultValue
            or HiddenInputValueSources.WebPageID
            or HiddenInputValueSources.WebPageURL
            or HiddenInputValueSources.ChannelID
            or _ => $"<em>{sourceDisplayName}</em>"
        });
    }
}

public class HiddenInputProperties : FormComponentProperties<string?>
{
    public HiddenInputProperties() : base(FieldDataType.Text) { }

    [TextInputComponent(Label = "Default value", Order = EditingComponentOrder.DEFAULT_VALUE)]
    public override string? DefaultValue { get; set; } = "";

    [DropDownComponent(
        Label = "Value source",
        ExplanationText = """
            <p>The source of the value stored in the hidden input.
            Selecting an option can expose additional fields for configuration.</p>
            <p>Be careful storing sensitive information (e.g. URL query parameters or cookies) in the form submission.</p>
            """,
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(EnumDropDownOptionsProvider<HiddenInputValueSources>),
        Order = 1
    )]
    public string HiddenInputValueSource { get; set; } = nameof(HiddenInputValueSources.DefaultValue);
    public HiddenInputValueSources HiddenInputValueSourceParsed => EnumDropDownOptionsProvider<HiddenInputValueSources>.Parse(HiddenInputValueSource, HiddenInputValueSources.DefaultValue);

    [TextInputComponent(
        Label = "Query string parameter",
        ExplanationText = "The query string parameter values are retrieved from.",
        Tooltip = "Example: utm_campaign",
        Order = 2
    )]
    [VisibleIfEqualTo(
        nameof(HiddenInputValueSource),
        nameof(HiddenInputValueSources.QueryStringValue),
        StringComparison.OrdinalIgnoreCase
    )]
    public string QueryStringParameterName { get; set; } = "";

    [TextInputComponent(
        Label = "Cookie name",
        ExplanationText = "The browser cookie values are retrieved from.",
        Tooltip = "Example: CMSCookieLevel",
        Order = 2
    )]
    [VisibleIfEqualTo(
        nameof(HiddenInputValueSource),
        nameof(HiddenInputValueSources.CookieValue),
        StringComparison.OrdinalIgnoreCase
    )]
    public string CookieName { get; set; } = "";
}

public enum HiddenInputValueSources
{
    [Description("Default value")]
    DefaultValue,
    [Description("Query string value")]
    QueryStringValue,
    [Description("Web page ID")]
    WebPageID,
    [Description("Web page URL")]
    WebPageURL,
    [Description("Cookie value")]
    CookieValue,
    [Description("Channel ID")]
    ChannelID,
}
