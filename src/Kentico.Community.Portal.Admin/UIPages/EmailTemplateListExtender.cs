using CMS.EmailLibrary;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(EmailTemplateListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class EmailTemplateListExtender : PageExtender<EmailTemplateList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        _ = Page.PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(EmailTemplateInfo.EmailTemplateDescription), caption: "Description", minWidth: 30);
    }
}
