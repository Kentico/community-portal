using CMS.DataEngine;
using Kentico.Community.Portal.Web.Components.FormBuilder;
using Kentico.Forms.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.ComponentIcons;
using System.ComponentModel.DataAnnotations;

[assembly: RegisterFormComponent(
    LongAnswerTextAreaComponent.IDENTIFIER,
    typeof(LongAnswerTextAreaComponent),
    "Long answer textarea",
    Description = "Survey-friendly long-answer input with configurable textarea size.",
    IconClass = KenticoIcons.PARAGRAPH_CENTER,
    ViewName = "~/Components/FormBuilder/LongAnswerTextArea/LongAnswerTextArea.cshtml")]

namespace Kentico.Community.Portal.Web.Components.FormBuilder;

public class LongAnswerTextAreaComponent : FormComponent<LongAnswerTextAreaProperties, string>, IHideableComponent
{
    public const string IDENTIFIER = "CommunityPortal.FormComponent.LongAnswerTextArea";

    [BindableProperty]
    public string? Value { get; set; } = "";

    public override string LabelForPropertyName => nameof(Value);

    public bool IsHidden => Properties.IsHidden;

    public int Rows => Math.Clamp(Properties.Rows, 3, 20);

    public int? MinCharacters => Properties.MinCharacters > 0 ? Properties.MinCharacters : null;

    public int? MaxCharacters => Properties.MaxCharacters > 0 ? Math.Max(Properties.MaxCharacters, MinCharacters ?? 0) : null;

    public string CharacterHint
    {
        get
        {
            if (MinCharacters is null && MaxCharacters is null)
            {
                return "";
            }

            if (MinCharacters is not null && MaxCharacters is not null)
            {
                return $"Answer between {MinCharacters} and {MaxCharacters} characters.";
            }

            if (MinCharacters is not null)
            {
                return $"Answer with at least {MinCharacters} characters.";
            }

            return $"Answer with at most {MaxCharacters} characters.";
        }
    }

    public override string GetValue() => Value ?? "";

    public override void SetValue(string value) => Value = value;

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();
        errors.AddRange(base.Validate(validationContext));

        string value = Value ?? "";

        if (MinCharacters is not null && value.Length < MinCharacters)
        {
            errors.Add(new ValidationResult($"Please enter at least {MinCharacters} characters."));
        }

        if (MaxCharacters is not null && value.Length > MaxCharacters)
        {
            errors.Add(new ValidationResult($"Please enter no more than {MaxCharacters} characters."));
        }

        return errors;
    }
}

public class LongAnswerTextAreaProperties : FormComponentProperties<string>
{
    [TextInputComponent(
        Label = "Default value",
        Order = 0)]
    public override string DefaultValue { get; set; } = "";

    [NumberInputComponent(
        Label = "Textarea rows",
        ExplanationText = "Configures the visible height of the textarea.",
        Order = 1)]
    public int Rows { get; set; } = 6;

    [NumberInputComponent(
        Label = "Minimum characters",
        ExplanationText = "Optional minimum character count for responses.",
        Order = 2)]
    public int MinCharacters { get; set; }

    [NumberInputComponent(
        Label = "Maximum characters",
        ExplanationText = "Optional maximum character count for responses.",
        Order = 3)]
    public int MaxCharacters { get; set; }

    [CheckBoxComponent(
        Label = "Is hidden?",
        ExplanationText = "If true, this field will not be rendered in the form. It must not be marked as <strong>required</strong>.",
        ExplanationTextAsHtml = true,
        Order = 100
    )]
    public bool IsHidden { get; set; } = false;

    public LongAnswerTextAreaProperties()
        : base(FieldDataType.LongText)
    {
    }
}
