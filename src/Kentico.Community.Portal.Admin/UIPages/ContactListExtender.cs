using CMS.ContactManagement;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(ContactListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class ContactListExtender : PageExtender<ContactList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var configs = Page.PageConfiguration.ColumnConfigurations;

        configs
            .TryFirst(c => string.Equals(c.Name, nameof(ContactInfo.ContactLastName), StringComparison.OrdinalIgnoreCase))
            .Execute(c => c.Sorting.DefaultDirection = null);
        configs
            .TryFirst(c => string.Equals(c.Name, nameof(ContactInfo.ContactCreated), StringComparison.OrdinalIgnoreCase))
            .Execute(c => c.Sorting.DefaultDirection = SortTypeEnum.Desc);
    }
}

