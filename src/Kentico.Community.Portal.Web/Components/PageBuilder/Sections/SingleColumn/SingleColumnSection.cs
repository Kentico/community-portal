using System.ComponentModel;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Sections.SingleColumn;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterSection(
    identifier: SingleColumnSection.IDENTIFIER,
    viewComponentType: typeof(SingleColumnSection),
    name: "1 column",
    propertiesType: typeof(SingleColumnSectionProperties),
    Description = "Single-column section with one full-width zone.",
    IconClass = KenticoIcons.SQUARE)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Sections.SingleColumn;

public class SingleColumnSection : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.SingleColumnSection";

    public IViewComponentResult Invoke(ComponentViewModel<SingleColumnSectionProperties> vm)
    {
        var model = new SingleColumnSectionViewModel(vm.Properties);

        return View("~/Components/PageBuilder/Sections/SingleColumn/SingleColumn.cshtml", model);
    }
}

public class SingleColumnSectionProperties : ISectionProperties
{
    [DropDownComponent(
        Label = "Layout",
        ExplanationText = "Determines the layout of the section columns",
        Tooltip = "Select a layout",
        DataProviderType = typeof(EnumDropDownOptionsProvider<Layouts>),
        Order = 3
    )]
    public string Layout { get; set; } = nameof(Layouts.Standard);
    public Layouts LayoutParsed => EnumDropDownOptionsProvider<Layouts>.Parse(Layout, Layouts.Standard);

    [DropDownComponent(
        Label = "Alignment",
        ExplanationText = "The alignment of content within the section. Individual widgets can override this.",
        Tooltip = "Select an alignment",
        DataProviderType = typeof(EnumDropDownOptionsProvider<Alignments>),
        Order = 4
    )]
    public string Alignment { get; set; } = nameof(Alignments.Left);
    public Alignments AlignmentParsed => EnumDropDownOptionsProvider<Alignments>.Parse(Alignment, Alignments.Left);

    [DropDownComponent(
        Label = "Padding Top",
        ExplanationText = "Determines the amount of padding at the top of the section",
        Tooltip = "Select padding",
        DataProviderType = typeof(EnumDropDownOptionsProvider<VerticalPaddings>),
        Order = 5
    )]
    public string PaddingTop { get; set; } = nameof(VerticalPaddings.Large);
    public VerticalPaddings PaddingTopParsed => EnumDropDownOptionsProvider<VerticalPaddings>.Parse(PaddingTop, VerticalPaddings.Large);

    [DropDownComponent(
        Label = "Padding Bottom",
        ExplanationText = "Determines the amount of padding at the bottom of the section",
        Tooltip = "Select padding",
        DataProviderType = typeof(EnumDropDownOptionsProvider<VerticalPaddings>),
        Order = 6
    )]
    public string PaddingBottom { get; set; } = nameof(VerticalPaddings.Large);
    public VerticalPaddings PaddingBottomParsed => EnumDropDownOptionsProvider<VerticalPaddings>.Parse(PaddingBottom, VerticalPaddings.Large);

    [DropDownComponent(
        Label = "Background Color",
        ExplanationText = "Determines the background color of the entire section",
        Tooltip = "Select a Background Color",
        DataProviderType = typeof(EnumDropDownOptionsProvider<BackgroundColors>),
        Order = 7
    )]
    public string BackgroundColor { get; set; } = nameof(BackgroundColors.White);
    public BackgroundColors BackgroundColorParsed => EnumDropDownOptionsProvider<BackgroundColors>.Parse(BackgroundColor, BackgroundColors.White);
}

public enum Layouts
{
    [Description("Standard")]
    Standard,
    [Description("Full width - Content contained")]
    FullWidth_ContentContained,
    [Description("Full width - Edge to edge")]
    FullWidth_EdgeToEdge,
}

public enum Alignments
{
    [Description("Left")]
    Left,
    [Description("Center")]
    Center,
    [Description("Right")]
    Right,
}


public enum VerticalPaddings
{
    [Description("Large")]
    Large,
    [Description("Medium")]
    Medium,
    [Description("Small")]
    Small,
    [Description("None")]
    None,
}

public enum BackgroundColors
{
    [Description("Light")]
    Light,
    [Description("White")]
    White,
    [Description("Dark")]
    Dark,
    [Description("Secondary Light")]
    Secondary_Light
}

public class SingleColumnSectionViewModel
{
    public SingleColumnSectionViewModel(SingleColumnSectionProperties props)
    {
        Layout = props.LayoutParsed;
        Alignment = props.AlignmentParsed;
        PaddingTop = props.PaddingTopParsed;
        PaddingBottom = props.PaddingBottomParsed;
        BackgroundColor = props.BackgroundColorParsed;
    }

    public Layouts Layout { get; set; }
    public Alignments Alignment { get; set; }
    public VerticalPaddings PaddingTop { get; set; }
    public VerticalPaddings PaddingBottom { get; set; }
    public BackgroundColors BackgroundColor { get; set; }
}
