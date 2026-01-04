using System.ComponentModel;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.FallbackForm;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Forms;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Forms.Web.Mvc.Widgets;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: FallbackFormWidget.IDENTIFIER,
    name: FallbackFormWidget.NAME,
    viewComponentType: typeof(FallbackFormWidget),
    propertiesType: typeof(FallbackFormWidgetProperties),
    Description = "Adds a form which can be hidden completely or disabled with a message.",
    IconClass = KenticoIcons.FORM)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.FallbackForm;

public class FallbackFormWidget(MarkdownRenderer renderer) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.FallbackFormWidget";
    public const string NAME = "Fallback form";
    private readonly MarkdownRenderer renderer = renderer;

    public IViewComponentResult Invoke(ComponentViewModel<FallbackFormWidgetProperties> vm)
    {
        var props = vm.Properties;

        return Validate(props)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/Forms/FallbackForm.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm)
            );
    }

    private Result<FallbackFormWidgetViewModel, ComponentErrorViewModel> Validate(FallbackFormWidgetProperties props)
    {
        if (props.SelectedForm.FirstOrDefault() is null)
        {
            return Result.Failure<FallbackFormWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No form has been selected."));
        }

        if (props.ComponentStateParsed == ComponentStates.Disabled
            && string.IsNullOrWhiteSpace(props.FallbackMarkdown)
            && (string.IsNullOrWhiteSpace(props.FallbackCTAPathOrURL) || string.IsNullOrWhiteSpace(props.FallbackCTALabel)))
        {
            return Result.Failure<FallbackFormWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "Fallback content or a CTA must be provided for a disabled form."));
        }

        return new FallbackFormWidgetViewModel(props, renderer);
    }
}

[FormCategory(Label = "Design", Order = 5)]
[FormCategory(Label = "Fallback", Order = 7)]
public class FallbackFormWidgetProperties : FormWidgetProperties
{
    [DropDownComponent(
        Label = "Background color",
        ExplanationText = "Determines the background color of the entire form",
        Tooltip = "Select a color",
        DataProviderType = typeof(EnumDropDownOptionsProvider<BackgroundColors>),
        Order = 6
    )]
    public string BackgroundColor { get; set; } = nameof(BackgroundColors.White);
    public BackgroundColors BackgroundColorParsed => EnumDropDownOptionsProvider<BackgroundColors>.Parse(BackgroundColor, BackgroundColors.White);

    [DropDownComponent(
        Label = "State",
        ExplanationText = """
            <p>The state of this variant of the widget.</p>
            <ul>
                <li>Hidden: the entire widget is completely hidden from the web page.</li>
                <li>Disabled: the form is visible but disabled with a fallback message.</li>
                <li>Active: the form functions normally and can be interacted with.</li>
            </ul>
            """,
        ExplanationTextAsHtml = true,
        DataProviderType = typeof(EnumDropDownOptionsProvider<ComponentStates>),
        Order = 6
    )]
    public string ComponentState { get; set; } = nameof(ComponentStates.Active);
    public ComponentStates ComponentStateParsed => EnumDropDownOptionsProvider<ComponentStates>.Parse(ComponentState, ComponentStates.Active);

    [MarkdownComponent(
        Label = "Fallback content",
        Order = 8,
        ExplanationText = "If specified, this will be displayed instead of a form. If no value is provided, the form will be completely hidden."
    )]
    [VisibleIfEqualTo(nameof(ComponentState), nameof(ComponentStates.Disabled))]
    public string FallbackMarkdown { get; set; } = "";

    [UrlSelectorComponent(
        Label = "CTA path or URL",
        Order = 9,
        ExplanationText = "If specified, a fallback CTA will be displayed linking to this path or URL."
    )]
    [VisibleIfEqualTo(nameof(ComponentState), nameof(ComponentStates.Disabled))]
    public string FallbackCTAPathOrURL { get; set; } = "";

    [TextInputComponent(
        Label = "CTA label",
        Order = 10,
        ExplanationText = "Required if CTA path or URL is specified"
    )]
    [VisibleIfEqualTo(nameof(ComponentState), nameof(ComponentStates.Disabled))]
    [VisibleIfNotEmpty(nameof(FallbackCTAPathOrURL))]
    public string FallbackCTALabel { get; set; } = "";
}

public enum ComponentStates
{
    [Description("Active")]
    Active,
    [Description("Disabled")]
    Disabled,
    [Description("Hidden")]
    Hidden,
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

public class FallbackFormWidgetViewModel : IHideableComponent
{
    public bool IsHidden { get; }
    public ComponentStates ComponentState { get; }
    public FormWidgetProperties FormWidgetProperties { get; }
    public BackgroundColors BackgroundColor { get; }
    public Maybe<HtmlString> FallbackContent { get; }
    public Maybe<LinkDataType> CTA { get; }

    public FallbackFormWidgetViewModel(FallbackFormWidgetProperties props, MarkdownRenderer renderer)
    {
        IsHidden = props.ComponentStateParsed != ComponentStates.Active;
        ComponentState = props.ComponentStateParsed;
        FallbackContent = Maybe.From(props.FallbackMarkdown)
            .MapNullOrWhiteSpaceAsNone()
            .Map(renderer.RenderUnsafe);
        CTA = string.IsNullOrWhiteSpace(props.FallbackCTALabel) || string.IsNullOrWhiteSpace(props.FallbackCTAPathOrURL)
            ? Maybe<LinkDataType>.None
            : new LinkDataType() { Label = props.FallbackCTALabel, URL = props.FallbackCTAPathOrURL };
        FormWidgetProperties = props;
        BackgroundColor = props.BackgroundColorParsed;
    }
}
