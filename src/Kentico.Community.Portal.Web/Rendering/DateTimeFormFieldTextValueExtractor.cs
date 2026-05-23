using System.Globalization;
using CMS.DataEngine;
using CMS.OnlineForms;

namespace Kentico.Community.Portal.Web.Rendering;

public class DateTimeFormFieldTextValueExtractor : FormFieldTextValueExtractorBase<DateTime>
{
    public override Task<bool> CanExtract(FormFieldExtractionContext context) =>
        Task.FromResult(string.Equals(
            context.FormFieldInfo.DataType,
            FieldDataType.DateTime,
            StringComparison.OrdinalIgnoreCase));

    public override Task<string> Extract(DateTime value, FormFieldExtractionContext context)
    {
        string format = value.TimeOfDay == TimeSpan.Zero
            ? "yyyy/MM/dd"
            : "yyyy/MM/dd HH:mm:ss";

        return Task.FromResult(value.ToString(format, CultureInfo.InvariantCulture));
    }
}
