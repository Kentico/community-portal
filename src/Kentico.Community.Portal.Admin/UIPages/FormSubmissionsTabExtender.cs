using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(FormSubmissionsTabExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class FormSubmissionsTabExtender : PageExtender<FormSubmissionsTab>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var configs = Page.PageConfiguration.ColumnConfigurations;

        configs
            .TryFirst(c => string.Equals(c.Name, "FormInserted", StringComparison.OrdinalIgnoreCase))
            .Execute(c => c.Sorting.DefaultDirection = SortTypeEnum.Desc);
    }
}
