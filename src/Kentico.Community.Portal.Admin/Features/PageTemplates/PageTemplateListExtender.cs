using CMS.Base;
using CMS.Websites;
using Kentico.Community.Portal.Admin.Features.PageTemplates;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Websites.UIPages;

[assembly: PageExtender(typeof(PageTemplateListExtender))]

namespace Kentico.Community.Portal.Admin.Features.PageTemplates;

public class PageTemplateListExtender : PageExtender<PageTemplateList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var config = Page.PageConfiguration;

        _ = config.ColumnConfigurations
            .AddColumn(
                nameof(PageTemplateConfigurationInfo.PageTemplateConfigurationLastModified),
                caption: "Modified",
                sortable: true,
                defaultSortDirection: SortTypeEnum.Desc,
                minWidth: 20)
            .AddComponentColumn(
                nameof(PageTemplateConfigurationInfo.PageTemplateConfigurationIcon),
                "@kentico-community/portal-web-admin/Icon",
                modelRetriever: IconModelRetriever,
                caption: "Icon",
                maxWidth: 5);
    }

    private static TableRowIconProps IconModelRetriever(object value, IDataContainer container) =>
        new(value?.ToString() ?? "");
}
