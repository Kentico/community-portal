using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Kentico.Xperience.ComponentIcons;

[assembly: RegisterWidget(
    identifier: RegistrationWidget.IDENTIFIER,
    viewComponentType: typeof(RegistrationWidget),
    name: "Register",
    propertiesType: typeof(RegisterWidgetProperties),
    Description = "Displays a registration form for new users.",
    IconClass = KenticoIcons.ADD_MODULE,
    AllowCache = false)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;

public class RegistrationWidget(MarkdownRenderer markdownRenderer) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.RegistrationWidget";

    private readonly MarkdownRenderer markdownRenderer = markdownRenderer;

    public IViewComponentResult Invoke(ComponentViewModel<RegisterWidgetProperties> cvm)
    {
        var vm = new RegisterWidgetViewModel
        {
            RegisterModel = new RegisterViewModel
            {
                Content = new RegisterFormContent(cvm.Properties, markdownRenderer)
            }
        };

        return View("~/Components/PageBuilder/Widgets/Registration/Registration.cshtml", vm);
    }
}

public class RegisterWidgetProperties : BaseWidgetProperties
{
    public const string DEFAULT_TERMS_LABEL = "I agree to the [Terms of Use](/terms){target=\"_blank\"} and have read the [Privacy notice](/privacy){target=\"_blank\"}.";
    public const string DEFAULT_NEWSLETTER_HELP_TEXT = "We'll subscribe you when you confirm your email address.";
    public const string DEFAULT_COOKIE_NOTE = "Membership works with necessary cookies only. Analytics and tracking are optional, in [Cookie settings](/cookies-policy#cookie-settings).";

    [MarkdownComponent(
        Label = "Terms of Use checkbox label",
        ExplanationText = """Markdown, one sentence (rendered inside the checkbox label, no paragraphs). Link /terms and /privacy; add {target="_blank"} after a link to open it in a new tab. Empty: the default text.""",
        Order = 1)]
    public string TermsLabel { get; set; } = DEFAULT_TERMS_LABEL;

    [MarkdownComponent(
        Label = "Newsletter help text",
        ExplanationText = "Markdown, shown under the newsletter checkbox (the consent text above it comes from the Email marketing consent). Empty: the default text.",
        Order = 2)]
    public string NewsletterHelpText { get; set; } = DEFAULT_NEWSLETTER_HELP_TEXT;

    [MarkdownComponent(
        Label = "Cookie note",
        ExplanationText = "Markdown, shown under the Register button. Keep it consistent with /cookies-policy. Empty: the default text.",
        Order = 3)]
    public string CookieNote { get; set; } = DEFAULT_COOKIE_NOTE;
}

/// <summary>
/// The widget's editable form text, rendered. Built from <see cref="RegisterWidgetProperties"/>
/// on page render and again by <see cref="RegistrationWidgetController"/> when it re-renders the form.
/// </summary>
public class RegisterFormContent(RegisterWidgetProperties props, MarkdownRenderer markdownRenderer)
{
    /// <summary>
    /// Round-tripped (data protected) with the form so the controller can re-render the same text
    /// </summary>
    public RegisterWidgetProperties Properties { get; } = props;

    public HtmlString TermsLabel { get; } = markdownRenderer.RenderInlineUnsafe(
        OrDefault(props.TermsLabel, RegisterWidgetProperties.DEFAULT_TERMS_LABEL));

    public HtmlString NewsletterHelpText { get; } = markdownRenderer.RenderUnsafe(
        OrDefault(props.NewsletterHelpText, RegisterWidgetProperties.DEFAULT_NEWSLETTER_HELP_TEXT));

    public HtmlString CookieNote { get; } = markdownRenderer.RenderUnsafe(
        OrDefault(props.CookieNote, RegisterWidgetProperties.DEFAULT_COOKIE_NOTE));

    private static string OrDefault(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value;
}

public class RegisterWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => "Register";
    public RegisterViewModel RegisterModel { get; set; } = new();
}
