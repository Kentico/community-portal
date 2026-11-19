using System.ComponentModel.DataAnnotations;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Sections.Grid;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using Kentico.Xperience.ComponentIcons;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

[assembly: RegisterSection(
    identifier: GridSection.IDENTIFIER,
    viewComponentType: typeof(GridSection),
    name: "Grid",
    propertiesType: typeof(GridSectionProperties),
    Description = "A customizable Grid Section.",
    IconClass = KenticoIcons.L_HEADER_LIST_IMG)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Sections.Grid;

public class GridSection : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Components.Sections.Grid.Grid-Section";

    public IViewComponentResult Invoke(ComponentViewModel<GridSectionProperties> cvm)
    {
        var vm = new GridSectionViewModel(cvm.Properties);

        return View("~/Components/PageBuilder/Sections/Grid/Grid.cshtml", vm);
    }
}

public class GridSectionProperties : ISectionProperties
{
    [TextInputComponent(
        Label = "Heading",
        ExplanationText = "The heading of the grid",
        Tooltip = "Enter a label",
        Order = 1
    )]
    public string Heading { get; set; } = "";

    [VisibleIfNotEmpty(nameof(Heading))]
    [DropDownComponent(
        Label = "Heading Alignment",
        ExplanationText = "The alignment of the heading text",
        Tooltip = "Select an alignment",
        DataProviderType = typeof(EnumDropDownOptionsProvider<Alignments>),
        Order = 2
    )]
    public string HeadingAlignment { get; set; } = nameof(Alignments.Left);
    public Alignments HeadingAlignmentParsed => EnumDropDownOptionsProvider<Alignments>.Parse(HeadingAlignment, Alignments.Left);

    [VisibleIfNotEmpty(nameof(Heading))]
    [DropDownComponent(
        Label = "Heading Spacing",
        ExplanationText = "The space between the heading and the section content",
        Tooltip = "Select a spacing",
        DataProviderType = typeof(EnumDropDownOptionsProvider<HeadingSpacings>),
        Order = 3
    )]
    public string HeadingSpacing { get; set; } = nameof(HeadingSpacings.Default);
    public HeadingSpacings HeadingSpacingParsed => EnumDropDownOptionsProvider<HeadingSpacings>.Parse(HeadingSpacing, HeadingSpacings.Default);

    [DropDownComponent(
        Label = "Body Alignment",
        ExplanationText = "The alignment of the body content",
        Tooltip = "Select an alignment",
        DataProviderType = typeof(EnumDropDownOptionsProvider<Alignments>),
        Order = 5
    )]
    public string BodyAlignment { get; set; } = nameof(Alignments.Left);
    public Alignments BodyAlignmentParsed => EnumDropDownOptionsProvider<Alignments>.Parse(BodyAlignment, Alignments.Left);

    [CheckBoxComponent(
        Label = "Is Heading Anchor Link Visible?",
        ExplanationText = "If true, the heading includes a clickable link to this portion of the page.",
        Order = 4
    )]
    public bool IsHeadingAnchorLinkVisible { get; set; }

    [DropDownComponent(
        Label = "Layout",
        ExplanationText = "Determines the layout of the section columns",
        Tooltip = "Select a layout",
        DataProviderType = typeof(EnumDropDownOptionsProvider<Layouts>),
        Order = 6
    )]
    public string Layout { get; set; } = nameof(Layouts._1_Column);
    public Layouts LayoutParsed => EnumDropDownOptionsProvider<Layouts>.Parse(Layout, Layouts._1_Column);

    [DropDownComponent(
        Label = "Columns Stack Below",
        ExplanationText = "The screen width below which a multi-column layout stacks into one column. Default: 2 columns stack below 768px, 3 columns below 992px.",
        Tooltip = "Select a breakpoint",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ColumnBreakpoints>),
        Order = 7
    )]
    public string ColumnBreakpoint { get; set; } = nameof(ColumnBreakpoints.Default);
    public ColumnBreakpoints ColumnBreakpointParsed => EnumDropDownOptionsProvider<ColumnBreakpoints>.Parse(ColumnBreakpoint, ColumnBreakpoints.Default);

    [DropDownComponent(
        Label = "Content Width",
        ExplanationText = "The maximum width of the section content. Not used by the full width layout.",
        Tooltip = "Select a width",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ContentWidths>),
        Order = 8
    )]
    public string ContentWidth { get; set; } = nameof(ContentWidths.Default);
    public ContentWidths ContentWidthParsed => EnumDropDownOptionsProvider<ContentWidths>.Parse(ContentWidth, ContentWidths.Default);

    [DropDownComponent(
        Label = "Padding Top",
        ExplanationText = "Determines the amount of padding at the top of the section",
        Tooltip = "Select padding",
        DataProviderType = typeof(EnumDropDownOptionsProvider<VerticalPaddings>),
        Order = 9
    )]
    public string PaddingTop { get; set; } = nameof(VerticalPaddings.Large);
    public VerticalPaddings PaddingTopParsed => EnumDropDownOptionsProvider<VerticalPaddings>.Parse(PaddingTop, VerticalPaddings.Large);

    [DropDownComponent(
        Label = "Padding Bottom",
        ExplanationText = "Determines the amount of padding at the bottom of the section",
        Tooltip = "Select padding",
        DataProviderType = typeof(EnumDropDownOptionsProvider<VerticalPaddings>),
        Order = 10
    )]
    public string PaddingBottom { get; set; } = nameof(VerticalPaddings.Large);
    public VerticalPaddings PaddingBottomParsed => EnumDropDownOptionsProvider<VerticalPaddings>.Parse(PaddingBottom, VerticalPaddings.Large);

    [NumberInputComponent(
        Label = "Number of Widget Zones",
        Tooltip = "Select the number of Widget Zones available",
        Order = 11
    )]
    [Range(1, 6)]
    public int WidgetZonesCount { get; set; } = 1;

    [DropDownComponent(
        Label = "Background Color",
        ExplanationText = "Determines the background color of the entire section",
        Tooltip = "Select a Background Color",
        DataProviderType = typeof(EnumDropDownOptionsProvider<SectionBackgroundColors>),
        Order = 12
    )]
    public string BackgroundColor { get; set; } = nameof(SectionBackgroundColors.White);
    public SectionBackgroundColors BackgroundColorParsed => EnumDropDownOptionsProvider<SectionBackgroundColors>.Parse(BackgroundColor, SectionBackgroundColors.White);
}

public enum Alignments
{
    [Description("Left")]
    Left,
    [Description("Center")]
    Center
}

public enum HeadingSpacings
{
    [Description("Default")]
    Default,
    [Description("Medium")]
    Medium,
    [Description("Large")]
    Large
}

public enum ColumnBreakpoints
{
    [Description("Default")]
    Default,
    [Description("Large (992px)")]
    Large,
    [Description("Extra Large (1200px)")]
    Extra_Large
}

public enum ContentWidths
{
    [Description("Default")]
    Default,
    [Description("Narrow")]
    Narrow
}

public enum Layouts
{
    [Description("1 Column (Narrow)")]
    _1_Column_Narrow,
    [Description("1 Column (Max Width Limited)")]
    _1_Column_Max_Width_Limited,
    [Description("1 Column")]
    _1_Column,
    [Description("1 Column (Full Width)")]
    _1_Column_Full_Width,
    [Description("2 Columns (50/50)")]
    _2_Columns_50_50,
    [Description("3 Columns (33/33/33)")]
    _3_Columns_33_33_33
}

public enum VerticalPaddings
{
    [Description("Large")]
    Large,
    [Description("Medium Large")]
    Medium_Large,
    [Description("Medium")]
    Medium,
    [Description("Small")]
    Small,
    [Description("None")]
    None,
}

public class GridSectionViewModel(GridSectionProperties props)
{
    public string? Heading { get; } = string.IsNullOrWhiteSpace(props.Heading) ? null : props.Heading;
    public Alignments HeadingAlignment { get; } = props.HeadingAlignmentParsed;
    public HeadingSpacings HeadingSpacing { get; } = props.HeadingSpacingParsed;
    public Alignments BodyAlignment { get; } = props.BodyAlignmentParsed;
    public ContentWidths ContentWidth { get; } = props.ContentWidthParsed;
    public ColumnBreakpoints ColumnBreakpoint { get; } = props.ColumnBreakpointParsed;
    public bool IsHeadingAnchorLinkVisible { get; } = props.IsHeadingAnchorLinkVisible;
    public VerticalPaddings PaddingTop { get; } = props.PaddingTopParsed;
    public VerticalPaddings PaddingBottom { get; } = props.PaddingBottomParsed;
    public Layouts Layout { get; } = props.LayoutParsed;
    public int WidgetZoneCount { get; } = props.WidgetZonesCount;
    public SectionBackgroundColors BackgroundColor { get; } = props.BackgroundColorParsed;
}
