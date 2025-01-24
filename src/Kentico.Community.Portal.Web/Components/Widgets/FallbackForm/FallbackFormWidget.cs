using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.Widgets.FallbackForm;
using Kentico.Forms.Web.Mvc.Widgets;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

[assembly: RegisterWidget(
    FallbackFormWidget.IDENTIFIER,
    FallbackFormWidget.WIDGET_NAME,
    typeof(FallbackFormWidgetProperties),
    customViewName: "~/Components/Widgets/FallbackForm/FallbackForm.cshtml",
    IconClass = KenticoIcons.FORM)]

namespace Kentico.Community.Portal.Web.Components.Widgets.FallbackForm;

public class FallbackFormWidget
{
    public const string IDENTIFIER = "CommunityPortal.FallbackFormWidget";
    public const string WIDGET_NAME = "Fallback form";
}

public class FallbackFormWidgetProperties : FormWidgetProperties
{
    [CheckBoxComponent(
        Label = "Is hidden?",
        Order = 0,
        ExplanationText = "If true, the specified fallback text value will be displayed."
    )]
    public bool IsHidden { get; set; }

    [MarkdownComponent(
        Label = "Fallback content",
        Order = 1,
        ExplanationText = "If specified, this will be displayed instead of a form. If no value is provided, nothing will be displayed."
    )]
    [VisibleIfTrue(nameof(IsHidden))]
    public string FallbackMarkdown { get; set; } = "";
}
