using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.Features.Forms;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(FormSubmissionsTabExtender))]

namespace Kentico.Community.Portal.Admin.Features.Forms;

/// <summary>
/// Sorts submissions newest first. Export is native (form submissions listing).
/// </summary>
public class FormSubmissionsTabExtender : PageExtender<FormSubmissionsTab>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        Page.PageConfiguration.ColumnConfigurations
            .TryFirst(c => string.Equals(c.Name, "FormInserted", StringComparison.OrdinalIgnoreCase))
            .Tap(c => c.Sorting.DefaultDirection = SortTypeEnum.Desc);
    }
}
