using CMS.EmailLibrary;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(EmailListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class EmailListExtender : PageExtender<EmailList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var configs = Page.PageConfiguration.ColumnConfigurations;

        configs
            .TryFirst(c => string.Equals(c.Name, nameof(EmailConfigurationInfo.EmailConfigurationLastModified), StringComparison.OrdinalIgnoreCase))
            .Execute(c =>
            {
                c.Sorting = new SortingConfiguration
                {
                    Sortable = true,
                    DefaultDirection = SortTypeEnum.Desc
                };
            });
    }
}

