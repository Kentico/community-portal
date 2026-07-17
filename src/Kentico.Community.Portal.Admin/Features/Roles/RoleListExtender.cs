using CMS.Membership;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.Features.Roles;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: PageExtender(typeof(RoleListExtender))]

namespace Kentico.Community.Portal.Admin.Features.Roles;

public class RoleListExtender : PageExtender<RoleList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var configs = Page.PageConfiguration.ColumnConfigurations;

        configs
            .TryFirst(c => string.Equals(c.Name, nameof(RoleInfo.RoleDisplayName), StringComparison.OrdinalIgnoreCase))
            .Tap(c =>
            {
                c.MinWidth = 20;
                c.MaxWidth = 30;
            });

        _ = configs.AddColumn(
            nameof(RoleInfo.RoleDescription),
            caption: "Description",
            minWidth: 30);
    }
}
