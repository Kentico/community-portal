using Kentico.Community.Portal.Web.Components.FormComponents;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Forms.Web.Mvc;
using CMS.DataEngine;
using Kentico.Community.Portal.Web.Components;

[assembly: RegisterFormComponent(
    DateInputComponent.IDENTIFIER,
    typeof(DateInputComponent),
    "Date input",
    Description = "Allows date input using the native HTML5 date input type",
    IconClass = KenticoIcons.CALENDAR_NUMBER,
    ViewName = "~/Components/FormComponents/DateInput/DateInput.cshtml")]

namespace Kentico.Community.Portal.Web.Components.FormComponents;

/// <summary>
/// Represents a single line input form component.
/// </summary>
public class DateInputComponent(TimeProvider timeProvider) : FormComponent<DateInputProperties, DateTime?>
{
    private string valueInternal = "";

    public const string IDENTIFIER = "CommunityPortal.FormComponent.DateInput";
    private readonly TimeProvider timeProvider = timeProvider;

    [BindableProperty]
    public string Value
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(valueInternal))
            {
                return valueInternal;
            }

            if (Properties.UseCurrentDateAsDefault)
            {
                return timeProvider.GetLocalNow().Date.ToString("yyyy-MM-dd");
            }

            if (Properties.DefaultValue.HasValue)
            {
                return Properties.DefaultValue.Value.ToString("yyyy-MM-dd");
            }

            return valueInternal;
        }
        set => valueInternal = value;
    }

    public override string LabelForPropertyName => nameof(Value);

    public override DateTime? GetValue() => DateTime.TryParse(Value, out var date) ? date : DateTime.MinValue;

    public override void SetValue(DateTime? value) => Value = value?.Date.ToString("yyyy-MM-dd") ?? "";
}

public class DateInputProperties : FormComponentProperties<DateTime?>
{
    public override DateTime? DefaultValue { get; set; }

    [CheckBoxComponent(
        Label = "Use current date as default",
        ExplanationText = "Populates the date field with the current date when the form is rendered",
        Order = 100)]
    public bool UseCurrentDateAsDefault { get; set; } = true;

    public DateInputProperties()
        : base(FieldDataType.DateTime)
    {
    }
}
