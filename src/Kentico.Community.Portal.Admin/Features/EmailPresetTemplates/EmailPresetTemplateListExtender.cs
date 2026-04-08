using CMS.Base;
using CMS.EmailLibrary.Internal;
using Kentico.Community.Portal.Admin.Features.EmailPresetTemplates;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(EmailPresetTemplateListExtender))]

namespace Kentico.Community.Portal.Admin.Features.EmailPresetTemplates;

public class EmailPresetTemplateListExtender : PageExtender<EmailPresetTemplateList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var config = Page.PageConfiguration;

        _ = config.ColumnConfigurations
            .AddColumn(
                nameof(EmailPresetTemplateInfo.EmailPresetTemplateLastModified),
                caption: "Modified",
                sortable: true,
                defaultSortDirection: SortTypeEnum.Desc,
                minWidth: 20)
            .AddComponentColumn(
                nameof(EmailPresetTemplateInfo.EmailPresetTemplateIcon),
                "@kentico-community/portal-web-admin/Icon",
                modelRetriever: IconModelRetriever,
                caption: "Icon",
                maxWidth: 5);
    }

    private static TableRowIconProps IconModelRetriever(object value, IDataContainer container) =>
        new(value?.ToString() ?? "");
}
