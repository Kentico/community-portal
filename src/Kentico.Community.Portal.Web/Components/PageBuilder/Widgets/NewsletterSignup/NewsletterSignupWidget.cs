using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.NewsletterSignup;
using Kentico.Community.Portal.Web.Rendering;
using Microsoft.AspNetCore.Html;
using Kentico.Forms.Web.Mvc.Widgets;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.ComponentIcons;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: NewsletterSignupWidget.IDENTIFIER,
    name: NewsletterSignupWidget.NAME,
    viewComponentType: typeof(NewsletterSignupWidget),
    propertiesType: typeof(NewsletterSignupWidgetProperties),
    Description = "Newsletter sign-up form with the Email marketing consent text. No consent checkbox: the double opt-in click is the agreement.",
    IconClass = KenticoIcons.NEWSPAPER)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.NewsletterSignup;

/// <summary>
/// The <see cref="FORM_CODE_NAME"/> form, introduced by the Email marketing consent short text,
/// so the sign-up shows the same text the agreement records.
/// </summary>
public class NewsletterSignupWidget(MarkdownRenderer markdownRenderer) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.NewsletterSignupWidget";
    public const string NAME = "Newsletter sign-up";
    public const string FORM_CODE_NAME = "NewsletterSignup";

    private readonly MarkdownRenderer markdownRenderer = markdownRenderer;

    private const string VIEW_DEFAULT = "~/Components/PageBuilder/Widgets/NewsletterSignup/NewsletterSignup.cshtml";
    private const string VIEW_BANNER = "~/Components/PageBuilder/Widgets/NewsletterSignup/NewsletterSignupBanner.cshtml";

    public IViewComponentResult Invoke(ComponentViewModel<NewsletterSignupWidgetProperties> vm)
    {
        var model = new NewsletterSignupWidgetViewModel(vm.Properties, markdownRenderer);

        return View(model.Design == NewsletterSignupDesigns.Banner ? VIEW_BANNER : VIEW_DEFAULT, model);
    }
}

public class NewsletterSignupWidgetProperties : BaseWidgetProperties
{
    public const string DEFAULT_HEADING = "Kentico Community news and updates";
    public const string DEFAULT_THANK_YOU = "Check your inbox for a confirmation link to finish subscribing.";
    public const string DEFAULT_HELP_TEXT = "We'll email you a confirmation link. You're subscribed only after you click it.";
    public const string DEFAULT_TRACKING_NOTE = "We measure opens and clicks of every email we send. If you allow tracking in [Cookie settings](/cookies-policy#cookie-settings), they're also linked to your visitor record. [Details](/privacy#email-statistics)";

    [DropDownComponent(
        Label = "Design",
        ExplanationText = "Banner: full-width purple call to action with the copy beside the form.",
        DataProviderType = typeof(EnumDropDownOptionsProvider<NewsletterSignupDesigns>),
        Order = 0)]
    public string Design { get; set; } = nameof(NewsletterSignupDesigns.Default);
    public NewsletterSignupDesigns DesignParsed => EnumDropDownOptionsProvider<NewsletterSignupDesigns>.Parse(Design, NewsletterSignupDesigns.Default);

    [TextInputComponent(
        Label = "Heading",
        ExplanationText = "Optional. Leave empty when the section already has a heading.",
        Order = 1)]
    public string Heading { get; set; } = DEFAULT_HEADING;

    [MarkdownComponent(
        Label = "Description",
        ExplanationText = "Markdown, shown under the heading, beside the form. Empty: the help text is shown there instead.",
        Order = 2)]
    [VisibleIfEqualTo(nameof(Design), nameof(NewsletterSignupDesigns.Banner), StringComparison.OrdinalIgnoreCase)]
    public string Description { get; set; } = "";

    [TextInputComponent(
        Label = "Thank-you message",
        ExplanationText = "Shown after the form is submitted.",
        Order = 3)]
    public string ThankYouMessage { get; set; } = DEFAULT_THANK_YOU;

    [MarkdownComponent(
        Label = "Help text",
        ExplanationText = "Markdown, shown under the form. Explain the double opt-in. Empty: the default text.",
        Order = 4)]
    public string HelpText { get; set; } = DEFAULT_HELP_TEXT;

    [CheckBoxComponent(
        Label = "Show email tracking note",
        ExplanationText = "Explains email open and click measurement, and that Cookie settings decide whether it's linked to the visitor record.",
        Order = 5)]
    public bool ShowTrackingNote { get; set; } = true;

    [MarkdownComponent(
        Label = "Email tracking note",
        ExplanationText = "Markdown. Keep it consistent with /privacy (email statistics) and /cookies-policy. Empty: the default text.",
        Order = 6)]
    [VisibleIfTrue(nameof(ShowTrackingNote))]
    public string TrackingNote { get; set; } = DEFAULT_TRACKING_NOTE;
}

public enum NewsletterSignupDesigns
{
    Default,
    Banner,
}

public class NewsletterSignupWidgetViewModel(NewsletterSignupWidgetProperties props, MarkdownRenderer markdownRenderer) : BaseWidgetViewModel
{
    protected override string WidgetName => NewsletterSignupWidget.NAME;

    public NewsletterSignupDesigns Design { get; } = props.DesignParsed;

    public string Heading { get; } = props.Heading;

    /// <summary>
    /// Banner design only: marketing copy beside the form. When set, the help text moves under the form.
    /// </summary>
    public Maybe<HtmlString> Description { get; } = string.IsNullOrWhiteSpace(props.Description)
        ? Maybe<HtmlString>.None
        : markdownRenderer.RenderUnsafe(props.Description);

    public HtmlString HelpText { get; } = markdownRenderer.RenderUnsafe(
        string.IsNullOrWhiteSpace(props.HelpText) ? NewsletterSignupWidgetProperties.DEFAULT_HELP_TEXT : props.HelpText);

    public Maybe<HtmlString> TrackingNote { get; } = props.ShowTrackingNote
        ? markdownRenderer.RenderUnsafe(
            string.IsNullOrWhiteSpace(props.TrackingNote) ? NewsletterSignupWidgetProperties.DEFAULT_TRACKING_NOTE : props.TrackingNote)
        : Maybe<HtmlString>.None;

    public FormWidgetProperties FormWidgetProperties { get; } = new()
    {
        SelectedForm = [new ObjectRelatedItem { ObjectCodeName = NewsletterSignupWidget.FORM_CODE_NAME }],
        AfterSubmitMode = "message",
        AfterSubmitDisplayText = string.IsNullOrWhiteSpace(props.ThankYouMessage)
            ? NewsletterSignupWidgetProperties.DEFAULT_THANK_YOU
            : props.ThankYouMessage,
    };
}
