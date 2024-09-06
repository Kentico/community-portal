using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Kentico.Community.Portal.Web.Components.Widgets.Heading;
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
        Tooltip = "Select padding",
        DataProviderType = typeof(EnumDropDownOptionsProvider<HeadingLevels>),
        Order = 2
    )]
    public string HeadingLevel { get; set; } = nameof(HeadingLevels.H2);
    public HeadingLevels HeadingLevelsParsed => EnumDropDownOptionsProvider<HeadingLevels>.Parse(HeadingLevel, HeadingLevels.H2);

    [CheckBoxComponent(
        Label = "Show heading anchor?",
        ExplanationText = "If true, an anchor link will be displayed adjacent to the heading.",
        Order = 3
    )]
    public bool ShowHeadingAnchor { get; set; } = true;
}

public enum HeadingLevels
{
    H1, H2, H3, H4, H5, H6
}

public class HeadingWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = HeadingWidget.NAME;

    public HeadingLevels HeadingLevel { get; }
    public string HeadingText { get; }
    public Maybe<string> HeadingAnchorSlug { get; }

    public HeadingWidgetViewModel(HeadingWidgetProperties props, ISlugHelper slugHelper)
    {
        HeadingText = props.HeadingText;
        HeadingLevel = props.HeadingLevelsParsed;
        HeadingAnchorSlug = props.ShowHeadingAnchor
            ? slugHelper.GenerateSlug(HeadingText)
            : Maybe.None;
    }

}
