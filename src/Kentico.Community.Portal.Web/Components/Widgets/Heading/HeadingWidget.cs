using Kentico.Community.Portal.Web.Components.Widgets.Heading;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using Slugify;

[assembly: RegisterWidget(
    identifier: HeadingWidget.IDENTIFIER,
    viewComponentType: typeof(HeadingWidget),
    name: "Heading",
    propertiesType: typeof(HeadingWidgetProperties),
    Description = "Displays a heading in the page.",
    IconClass = "icon-l-header-text",
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.Widgets.Heading;

public class HeadingWidget(ISlugHelper slugHelper) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.HeadingWidget";
    public const string NAME = "Heading";

    private readonly ISlugHelper slugHelper = slugHelper;

    public IViewComponentResult Invoke(ComponentViewModel<HeadingWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        return Validate(props)
            .Match(
                vm => View("~/Components/Widgets/Heading/Heading.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private Result<HeadingWidgetViewModel, ComponentErrorViewModel> Validate(HeadingWidgetProperties props)
    {
        if (string.IsNullOrWhiteSpace(props.HeadingText))
        {
            return Result.Failure<HeadingWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No heading has been set for this widget."));
        }

        return new HeadingWidgetViewModel(props, slugHelper);
    }
}

public class HeadingWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Text",
        ExplanationText = "Text of the heading",
        WatermarkText = "Your heading",
        Order = 1)]
    public string HeadingText { get; set; } = "";

    [DropDownComponent(
        Label = "Heading level",
        ExplanationText = "Sets the level of the heading",
        DataProviderType = typeof(EnumDropDownOptionsProvider<HeadingLevels>),
        Order = 2
    )]
    public string HeadingLevel { get; set; } = nameof(HeadingLevels.H2);
    public HeadingLevels HeadingLevelsParsed => EnumDropDownOptionsProvider<HeadingLevels>.Parse(HeadingLevel, HeadingLevels.H2);

    [DropDownComponent(
        Label = "Heading alignment",
        ExplanationText = "Sets the alignment of the heading",
        DataProviderType = typeof(EnumDropDownOptionsProvider<HeadingAlignments>),
        Order = 3
    )]
    public string HeadingAlignment { get; set; } = nameof(HeadingAlignments.Left);
    public HeadingAlignments HeadingAlignmentsParsed => EnumDropDownOptionsProvider<HeadingAlignments>.Parse(HeadingAlignment, HeadingAlignments.Left);

    [CheckBoxComponent(
        Label = "Show heading anchor?",
        ExplanationText = "If true, an anchor link will be displayed adjacent to the heading.",
        Order = 4
    )]
    public bool ShowHeadingAnchor { get; set; } = true;
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

    public HeadingWidgetViewModel(HeadingWidgetProperties props, ISlugHelper slugHelper)
    {
        HeadingText = props.HeadingText;
        HeadingAlignment = props.HeadingAlignmentsParsed;
        HeadingLevel = props.HeadingLevelsParsed;
        HeadingAnchorSlug = props.ShowHeadingAnchor
            ? slugHelper.GenerateSlug(HeadingText)
            : Maybe.None;
    }
}
