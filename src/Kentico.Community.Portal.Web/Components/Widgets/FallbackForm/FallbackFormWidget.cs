using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.Widgets.FallbackForm;
using Kentico.Forms.Web.Mvc.Widgets;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

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

    public static bool GetIsFormHidden<T>(ViewDataDictionary<T> viewData) =>
        viewData.TryGetValue($"{IDENTIFIER}_IS_HIDDEN", out object? isHiddenObj) && isHiddenObj is bool isHidden && isHidden;

    /// <summary>
    /// Stores the state of the <see cref="FallbackFormWidget"/> in <see cref="ViewDataDictionary{TModel}"/>
    /// for child widgets and form components
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="viewData"></param>
    /// <param name="props"></param>
    public static void SetIsFormHidden<T>(ViewDataDictionary<T> viewData, FallbackFormWidgetProperties props) =>
        viewData.Add($"{IDENTIFIER}_IS_HIDDEN", props.IsHidden);

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
