using System.ComponentModel;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Markdown;
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
    IconClass = KenticoIcons.I,
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Markdown;

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
            MarkdownStyles.Note => props.Markdown,
            MarkdownStyles.Standard or _ => props.Markdown,
        };
        var html = markdownRenderer.RenderUnsafe(markdown);

        return Validate(props, html)
               .Match(
                   vm => View("~/Components/PageBuilder/Widgets/Markdown/Markdown.cshtml", vm),
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

        return new MarkdownWidgetViewModel(html, props);
    }
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 3)]
public class MarkdownWidgetProperties : BaseWidgetProperties
{
    [MarkdownComponent(
        Label = "Markdown",
        ExplanationText = "Markdown content that will be rendered on the page as HTML",
        Order = 2)]
    [TrackContentItemReference(typeof(MarkdownContentItemReferenceExtractor))]
    public string Markdown { get; set; } = "";

    [DropDownComponent(
        Label = "Style",
        ExplanationText = """
        <p>Sets the style of the rendered Markdown</p>
        <ul>
            <li>Standard - normal markdown (default)</li>
            <li>Note - wraps content in a styled alert box for emphasis</li>
            <li>Code - content is wrapped in a code block (deprecated - use /code tool in editor)</li>
        </ul>
        """,
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(EnumDropDownOptionsProvider<MarkdownStyles>),
        Order = 3
    )]
    public string MarkdownStyle { get; set; } = nameof(MarkdownStyles.Standard);
    public MarkdownStyles MarkdownStyleParsed => EnumDropDownOptionsProvider<MarkdownStyles>.Parse(MarkdownStyle, MarkdownStyles.Standard);

    [TextInputComponent(
        Label = "Code language",
        ExplanationText = "Example: csharp, sql, html, css, typescript",
        Order = 4)]
    [VisibleIfEqualTo(nameof(MarkdownStyle), nameof(MarkdownStyles.Code))]
    public string MarkdownCodeLanguage { get; set; } = "";

    [DropDownComponent(
        Label = "Note style",
        ExplanationText = "Visual style variant for the note",
        DataProviderType = typeof(EnumDropDownOptionsProvider<NoteStyles>),
        Order = 5
    )]
    [VisibleIfEqualTo(nameof(MarkdownStyle), nameof(MarkdownStyles.Note))]
    public string NoteStyle { get; set; } = nameof(NoteStyles.Info);
    public NoteStyles NoteStyleParsed => EnumDropDownOptionsProvider<NoteStyles>.Parse(NoteStyle, NoteStyles.Info);

    [CheckBoxComponent(
        Label = "Use default title",
        ExplanationText = "Uses default title based on note style (\"Note\" for Info, \"Warning\" for Warning)",
        Order = 6)]
    [VisibleIfEqualTo(nameof(MarkdownStyle), nameof(MarkdownStyles.Note))]
    public bool UseDefaultNoteTitle { get; set; }

    [TextInputComponent(
        Label = "Note title",
        ExplanationText = "Optional heading for the note (e.g., 'An important tip:')",
        Order = 7)]
    [VisibleIfEqualTo(nameof(MarkdownStyle), nameof(MarkdownStyles.Note))]
    [VisibleIfEqualTo(nameof(UseDefaultNoteTitle), false)]
    public string NoteTitle { get; set; } = "";
}

public enum MarkdownStyles
{
    [Description("Standard")]
    Standard,
    [Description("Note")]
    Note,
    [Description("Code (deprecated)")]
    Code
}

public enum NoteStyles
{
    [Description("Info")]
    Info,
    [Description("Warning")]
    Warning
}

public class MarkdownWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = MarkdownWidget.NAME;

    public HtmlString HTML { get; }
    public bool EnableNote { get; }
    public NoteStyles NoteStyle { get; }
    public string NoteTitle { get; }
    public bool HasNoteTitle { get; }

    public MarkdownWidgetViewModel(HtmlString html, MarkdownWidgetProperties properties)
    {
        HTML = html;
        EnableNote = properties.MarkdownStyleParsed == MarkdownStyles.Note;
        NoteStyle = properties.NoteStyleParsed;

        // Determine the note title
        string noteTitle = properties.UseDefaultNoteTitle
            ? GetDefaultNoteTitle(properties.NoteStyleParsed)
            : properties.NoteTitle;

        NoteTitle = noteTitle;
        HasNoteTitle = !string.IsNullOrWhiteSpace(noteTitle);
    }

    private static string GetDefaultNoteTitle(NoteStyles style) => style switch
    {
        NoteStyles.Info => "Note",
        NoteStyles.Warning => "Warning",
        _ => "Note"
    };
};
