using CMS.ContentEngine;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: PageExtender(typeof(TaxonomyListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class TaxonomyListExtender : PageExtender<TaxonomyList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        _ = Page.PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(TaxonomyInfo.TaxonomyDescription), caption: "Description", minWidth: 30);
    }
}
