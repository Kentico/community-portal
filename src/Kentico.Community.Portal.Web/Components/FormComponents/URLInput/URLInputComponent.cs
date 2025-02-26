using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.FormComponents;
using Kentico.Forms.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

[assembly: RegisterFormComponent(
    URLInputComponent.IDENTIFIER,
    typeof(URLInputComponent),
    "URL input",
    Description = "Allows an HTML5 validated URL as input.",
    IconClass = KenticoIcons.CHAIN,
    ViewName = "~/Components/FormComponents/URLInput/URLInput.cshtml")]

namespace Kentico.Community.Portal.Web.Components.FormComponents;

public class URLInputComponent : FormComponent<URLInputProperties, string>, IHideableComponent
{
    public const string IDENTIFIER = "CommunityPortal.FormComponent.URLInput";

    [BindableProperty]
    public string? Value { get; set; } = "";

    public override string LabelForPropertyName => nameof(Value);

    public bool IsHidden => Properties.IsHidden;

    public override string GetValue() => Value ?? "";

    public override void SetValue(string value) => Value = value;
}

public class URLInputProperties : TextInputProperties
{
    [CheckBoxComponent(
        Label = "Is hidden?",
        ExplanationText = "If true, this field will not be rendered in the form. It must not be marked as <strong>required</strong>.",
        ExplanationTextAsHtml = true,
        Order = 100
    )]
    public bool IsHidden { get; set; } = false;
}
