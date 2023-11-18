using CMS.Membership;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: PageExtender(typeof(MemberListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class MemberListExtender : PageExtender<MemberList>
{
    public override Task ConfigurePage()
    {
        _ = base.ConfigurePage();

        var configs = Page.PageConfiguration.ColumnConfigurations
                .AddColumn("MemberFirstName", caption: "First name")
                .AddColumn("MemberLastName", caption: "Last name");

        configs.Insert(0, new ColumnConfiguration
        {
            Name = "MemberID",
            Caption = "ID",
            MinWidth = 4,
            Sorting = new SortingConfiguration
            {
                Sortable = true
            },
        });

        configs
            .TryFirst(c => string.Equals(c.Name, nameof(MemberInfo.MemberName), StringComparison.OrdinalIgnoreCase))
            .Execute(c => c.Sorting.DefaultDirection = null);

        configs
            .TryFirst(c => string.Equals(c.Name, nameof(MemberInfo.MemberCreated)))
            .Execute(c => c.Sorting.DefaultDirection = SortTypeEnum.Desc);

        return Task.CompletedTask;
    }
}

