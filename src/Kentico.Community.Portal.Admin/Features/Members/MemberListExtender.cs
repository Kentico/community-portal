using CMS.Membership;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: PageExtender(typeof(MemberListExtender))]

namespace Kentico.Community.Portal.Admin.Features.Members;

public class MemberListExtender : PageExtender<MemberList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        Page.PageConfiguration.QueryModifiers.AddModifier((q, settings) =>
            MemberListFilter.ModifyQueryForBadgeFilter(q));

        var configs = Page.PageConfiguration.ColumnConfigurations
                .AddColumn("MemberFirstName", caption: "First name")
                .AddColumn("MemberLastName", caption: "Last name")
                .AddColumn("MemberModerationStatus", caption: "Moderation status");

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

        if (Page.PageConfiguration.FilterFormModel is null)
        {
            Page.PageConfiguration.FilterFormModel = new MemberListFilter();
        }
    }
}
