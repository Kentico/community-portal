using Kentico.Community.Portal.Web.Components.Widgets.Markdown;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: MarkdownWidget.IDENTIFIER,
    viewComponentType: typeof(MarkdownWidget),
    name: MarkdownWidget.NAME,
    propertiesType: typeof(MarkdownWidgetProperties),
    Description = "Renders the markdown content provided.",
    // https://devnet.kentico.com/docs/icon-list/index.html
    IconClass = "icon-rectangle-a",
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.Widgets.Markdown;

public class MarkdownWidget(MarkdownRenderer markdownRenderer) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.MarkdownWidget";
    public const string NAME = "Markdown";

    private readonly MarkdownRenderer markdownRenderer = markdownRenderer;

    public IViewComponentResult Invoke(ComponentViewModel<MarkdownWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        string markdown = props.MarkdownStyleParsed switch
        {
            MarkdownStyles.Code => $"""
                ```{props.MarkdownCodeLanguage}
                {props.Markdown}
                ```
                """,
            MarkdownStyles.Note => $"""
                :::note
                {props.Markdown}
                :::
                """,
            MarkdownStyles.Standard or _ => props.Markdown,
        };
        var html = markdownRenderer.RenderUnsafe(markdown);

        return Validate(props, html)
               .Match(
                   vm => View("~/Components/Widgets/Markdown/Markdown.cshtml", vm),
                   vm => View("~/Components/ComponentError.cshtml", vm)
               );
    }

    private static Result<MarkdownWidgetViewModel, ComponentErrorViewModel> Validate(MarkdownWidgetProperties props, HtmlString html)
    {
        if (string.IsNullOrWhiteSpace(props.Markdown))
        {
            return Result.Failure<MarkdownWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No markdown has been authored for this widget."));
        }

        if (string.IsNullOrWhiteSpace(html.Value))
        {
            return Result.Failure<MarkdownWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The given markdown generated no HTML."));
        }

        return new MarkdownWidgetViewModel(html);
    }
}

public class MarkdownWidgetProperties : BaseWidgetProperties
{
    [TextAreaComponent(
        Label = "Markdown",
        ExplanationText = "Markdown content that will be rendered on the page as HTML",
        MinRowsNumber = 30,
        WatermarkText = "## Add some markdown",
        Order = 1)]
    [TrackContentItemReference(typeof(MarkdownContentItemReferenceExtractor))]
    public string Markdown { get; set; } = "";

    [DropDownComponent(
        Label = "Style",
        ExplanationText = "Sets the style of the rendered Markdown",
        Tooltip = "Select a style",
        DataProviderType = typeof(EnumDropDownOptionsProvider<MarkdownStyles>),
        Order = 2
    )]
    public string MarkdownStyle { get; set; } = nameof(MarkdownStyles.Standard);
    public MarkdownStyles MarkdownStyleParsed => EnumDropDownOptionsProvider<MarkdownStyles>.Parse(MarkdownStyle, MarkdownStyles.Standard);

    [TextInputComponent(
        Label = "Code language",
        ExplanationText = "Example: csharp, sql, html, css, typescript",
        Order = 3)]
    [VisibleIfEqualTo(nameof(MarkdownStyle), nameof(MarkdownStyles.Code))]
    public string MarkdownCodeLanguage { get; set; } = "";
}

public enum MarkdownStyles
{
    Standard,
    Note,
    Code
}

public class MarkdownWidgetViewModel(HtmlString html) : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = MarkdownWidget.NAME;

    public HtmlString HTML { get; } = html;
};
