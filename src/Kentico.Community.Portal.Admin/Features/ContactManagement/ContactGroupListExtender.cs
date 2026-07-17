using CMS.ContactManagement;
using Kentico.Community.Portal.Admin.Features.ContactManagement;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(ContactGroupListExtender))]

namespace Kentico.Community.Portal.Admin.Features.ContactManagement;

public class ContactGroupListExtender : PageExtender<ContactGroupList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        _ = Page.PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(ContactGroupInfo.ContactGroupDescription), caption: "Description", minWidth: 30);
    }
}
