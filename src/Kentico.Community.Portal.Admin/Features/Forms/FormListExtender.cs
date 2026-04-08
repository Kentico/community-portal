using CMS.OnlineForms;
using Kentico.Community.Portal.Admin.Features.Forms;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(FormListExtender))]

namespace Kentico.Community.Portal.Admin.Features.Forms;

public class FormListExtender : PageExtender<FormList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        _ = Page.PageConfiguration.ColumnConfigurations.AddColumn(
            nameof(BizFormInfo.FormLastModified),
            caption: "Date Modified",
            defaultSortDirection: SortTypeEnum.Desc);
    }
}
