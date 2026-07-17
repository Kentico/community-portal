using CMS.Modules;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: PageExtender(typeof(ModuleListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class ModuleListExtender : PageExtender<ModuleList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        _ = Page.PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(ResourceInfo.ResourceDescription), caption: "Description", minWidth: 30);
    }
}
