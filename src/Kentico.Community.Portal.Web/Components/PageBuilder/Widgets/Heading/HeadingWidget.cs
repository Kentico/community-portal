using System.Text.Encodings.Web;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Heading;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: HeadingWidget.IDENTIFIER,
    viewComponentType: typeof(HeadingWidget),
    name: "Heading",
    propertiesType: typeof(HeadingWidgetProperties),
    Description = "Displays a heading in the page.",
    IconClass = KenticoIcons.L_HEADER_TEXT,
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Heading;

public class HeadingWidget(IHeadingContext headingContext) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.HeadingWidget";
    public const string NAME = "Heading";

    private readonly IHeadingContext headingContext = headingContext;

    public IViewComponentResult Invoke(ComponentViewModel<HeadingWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        return Validate(props)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/Heading/Heading.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private Result<HeadingWidgetViewModel, ComponentErrorViewModel> Validate(HeadingWidgetProperties props)
    {
        if (string.IsNullOrWhiteSpace(props.HeadingText))
        {
            return Result.Failure<HeadingWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No heading has been set for this widget."));
        }

        return new HeadingWidgetViewModel(props, headingContext);
    }
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 3)]
public class HeadingWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Text",
        ExplanationText = "Text of the heading",
        WatermarkText = "Your heading",
        Order = 2)]
    public string HeadingText { get; set; } = "";

    [DropDownComponent(
        Label = "Heading level",
        ExplanationText = "The HTML level of the heading (h1, h2, etc...)",
        DataProviderType = typeof(EnumDropDownOptionsProvider<HeadingLevels>),
        Order = 4
    )]
    public string HeadingLevel { get; set; } = nameof(HeadingLevels.H2);
    public HeadingLevels HeadingLevelsParsed => EnumDropDownOptionsProvider<HeadingLevels>.Parse(HeadingLevel, HeadingLevels.H2);

    [DropDownComponent(
        Label = "Heading alignment",
        ExplanationText = "The alignment of the heading",
        DataProviderType = typeof(EnumDropDownOptionsProvider<HeadingAlignments>),
        Order = 5
    )]
    public string HeadingAlignment { get; set; } = nameof(HeadingAlignments.Left);
    public HeadingAlignments HeadingAlignmentsParsed => EnumDropDownOptionsProvider<HeadingAlignments>.Parse(HeadingAlignment, HeadingAlignments.Left);

    [CheckBoxComponent(
        Label = "Show heading anchor?",
        ExplanationText = "If true, an anchor link will be displayed adjacent to the heading.",
        Order = 6
    )]
    public bool ShowHeadingAnchor { get; set; } = true;

    [TextInputComponent(
        Label = "Anchor override",
        ExplanationText = "Optional custom anchor ID. If empty, anchor is auto-generated from heading text. If populated, this exact value is used as the HTML id attribute. Must be unique on the page and contain only valid ID characters (letters, numbers, hyphens, underscores). No validation is performed - use with caution.",
        WatermarkText = "custom-anchor-id",
        Order = 7
    )]
    public string AnchorOverride { get; set; } = "";
}

public enum HeadingLevels { H1, H2, H3, H4, H5, H6 }
public enum HeadingAlignments { Left, Center, Right }

public class HeadingWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = HeadingWidget.NAME;

    public HeadingAlignments HeadingAlignment { get; }
    public HeadingLevels HeadingLevel { get; }
    public string HeadingText { get; }
    public Maybe<string> HeadingAnchorSlug { get; }

    public HeadingWidgetViewModel(HeadingWidgetProperties props, IHeadingContext headingContext)
    {
        HeadingText = props.HeadingText;
        HeadingAlignment = props.HeadingAlignmentsParsed;
        HeadingLevel = props.HeadingLevelsParsed;

        string rawSlug;
        if (!props.ShowHeadingAnchor)
        {
            HeadingAnchorSlug = Maybe.None;
            return;
        }
        else if (!string.IsNullOrWhiteSpace(props.AnchorOverride))
        {
            // Use custom anchor exactly as provided, bypassing auto-generation
            rawSlug = props.AnchorOverride.Trim();
        }
        else
        {
            // Auto-generate anchor from heading text with uniqueness logic
            rawSlug = headingContext.GetUniqueSlug(HeadingText);
        }

        // HTML encode the slug to prevent XSS
        HeadingAnchorSlug = HtmlEncoder.Default.Encode(rawSlug);
    }
}
