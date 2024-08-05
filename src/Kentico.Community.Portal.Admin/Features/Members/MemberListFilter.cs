using CMS.DataEngine;
using CMS.Membership;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base.Filters;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Community.Portal.Admin.Features.Members;

/// <summary>
/// Applies more complex filter conditions to member listing
/// </summary>
/// <remarks>
/// Should be used with <see cref="ModifyQueryForBadgeFilter"/> applied to the listing query
/// </remarks>
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

    [GeneralSelectorComponent(
        dataProviderType: typeof(MemberBadgeGeneralSelectorDataProvider),
        Label = "Badges",
        Placeholder = "Any",
        Order = 0
    )]
    [FilterCondition(
        BuilderType = typeof(MemberBadgeWhereConditionBuilder),
        ColumnName = nameof(MemberBadgeInfo.MemberBadgeCodeName)
    )]
    public IEnumerable<string> MemberBadges { get; set; } = [];

    public static ObjectQuery ModifyQueryForBadgeFilter(ObjectQuery query) =>
        query.Source(source =>
            source
                .LeftJoin(
                    // Ideally we'd apply this with a HAVING or CTE,
                    // but the filter query IWhereConditionBuilders only have access
                    // to the WHERE part of the SQL statement.
                    // A sub-query allows us to filter the results in the WHERE
                    sourceExpression: """
                        (SELECT
                            M.MemberID as Sub_MemberID,
                            STRING_AGG(B.MemberBadgeCodeName, ',') AS BadgeList
                        FROM
                            CMS_Member M
                            LEFT JOIN
                            KenticoCommunity_MemberBadgeMember MBM ON M.MemberID = MBM.MemberBadgeMemberMemberId
                            LEFT JOIN
                            KenticoCommunity_MemberBadge B ON MBM.MemberBadgeMemberMemberBadgeId = B.MemberBadgeID
                        GROUP BY 
                            M.MemberID) AS Badges
                        """,
                    condition: "Badges.Sub_MemberID = CMS_Member.MemberID"))
                .AddColumn(new QueryColumn("ISNULL(Badges.BadgeList, '')") { ColumnAlias = "BadgeList" });

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

public class MemberBadgeGeneralSelectorDataProvider(IMemberBadgeInfoProvider badgeProvider)
    : IGeneralSelectorDataProvider
{
    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Inavlid", Value = "" };
    private readonly IMemberBadgeInfoProvider badgeProvider = badgeProvider;

    public async Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        var badges = (await badgeProvider.GetAllMemberBadgesCached()).AsEnumerable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            badges = badges.Where(i => i.MemberBadgeDisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return new PagedSelectListItems<string>()
        {
            NextPageAvailable = false,
            Items = badges.Select(b => new ObjectSelectorListItem<string> { IsValid = true, Text = b.MemberBadgeDisplayName, Value = b.MemberBadgeCodeName }),
        };
    }

    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(IEnumerable<string> selectedValues, CancellationToken cancellationToken)
    {
        var badges = await badgeProvider.GetAllMemberBadgesCached();

        return (selectedValues ?? []).Select(GetSelectedItemByValue(badges));
    }

    private static Func<string, ObjectSelectorListItem<string>> GetSelectedItemByValue(IEnumerable<MemberBadgeInfo> badges) =>
        (string badgeName) => badges
            .TryFirst(b => string.Equals(b.MemberBadgeCodeName, badgeName, StringComparison.OrdinalIgnoreCase))
            .Map(b => new ObjectSelectorListItem<string> { IsValid = true, Text = b.MemberBadgeDisplayName, Value = b.MemberBadgeCodeName })
            .GetValueOrDefault(InvalidItem);
}

/// <summary>
/// Used with <see cref="MemberListFilter.ModifyQueryForBadgeFilter"/> to
/// filter member results by the badges assigned to members
/// </summary>
public class MemberBadgeWhereConditionBuilder : IWhereConditionBuilder
{
    public Task<IWhereCondition> Build(string columnName, object value)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            throw new ArgumentException(
                $"{nameof(columnName)} cannot be a null or an empty string.");
        }

        var whereCondition = new WhereCondition();

        if (value is null || value is not IEnumerable<string> memberBadges)
        {
            return Task.FromResult<IWhereCondition>(whereCondition);
        }

        string badgesToFilter = string.Join(",", memberBadges);

        string query = $"""
        SELECT COUNT(*)
        FROM STRING_SPLIT(@BadgesToFilter, ',') badge
        WHERE ISNULL(badges.BadgeList, '') LIKE '%' + badge.value + '%') 
            = (SELECT COUNT(*)
                FROM STRING_SPLIT(@BadgesToFilter, ',')
        """;

        _ = whereCondition.Where(query, new QueryDataParameters
        {
            { "BadgesToFilter", badgesToFilter }
        });

        return Task.FromResult<IWhereCondition>(whereCondition);
    }
}
