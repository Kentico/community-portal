using CMS.DataEngine;
using CMS.Membership;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Filters;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: PageExtender(typeof(MemberListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class MemberListExtender : PageExtender<MemberList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

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

        if (Page.PageConfiguration.FilterFormModel is null)
        {
            Page.PageConfiguration.FilterFormModel = new MemberListFilter();
        }
    }
}

public class MemberListFilter
{
    [DropDownComponent(
        Label = "Status",
        Options = "Enabled\r\nDisabled")]
    [FilterCondition(
        BuilderType = typeof(MemberStatusWhereConditionBuilder),
        ColumnName = nameof(MemberInfo.MemberEnabled)
    )]
    public string Status { get; set; } = "";
}

public class MemberStatusWhereConditionBuilder : IWhereConditionBuilder
{
    public Task<IWhereCondition> Build(string columnName, object value)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            throw new ArgumentException(
                $"{nameof(columnName)} cannot be a null or an empty string.");
        }

        var whereCondition = new WhereCondition();

        if (value is null || value is not string status)
        {
            return Task.FromResult<IWhereCondition>(whereCondition);
        }

        whereCondition = status switch
        {
            "Disabled" => new WhereCondition().WhereEquals(nameof(MemberInfo.MemberEnabled), 0),
            "Enabled" => new WhereCondition().WhereEquals(nameof(MemberInfo.MemberEnabled), 1),
            "" or _ => new WhereCondition("1 = 1"),
        };

        return Task.FromResult<IWhereCondition>(whereCondition);
    }
}
