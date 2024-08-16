using System.Collections.Immutable;
using System.Data;
using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.ContactManagement;
using Kentico.Community.Portal.Admin.Features.Reporting;
using Kentico.Community.Portal.Core;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: UIPage(
    uiPageType: typeof(ActivityStatsPage),
    parentType: typeof(ContactManagementApplication),
    slug: "reporting",
    name: "Reporting",
    icon: Icons.Graph,
    templateName: "@kentico-community/portal-web-admin/StatsLayout",
    order: 1000)]

namespace Kentico.Community.Portal.Admin.Features.ContactManagement;

public class ActivityStatsPage(ISystemClock clock) : Page<StatsLayoutClientProperties>
{
    public const string IDENTIFIER = "activity-stats";

    private readonly ISystemClock clock = clock;

    public override async Task<StatsLayoutClientProperties> ConfigureTemplateProperties(StatsLayoutClientProperties properties)
    {
        properties.TotalsTitle = "Activities by type";
        properties.Stats = await GetStats(properties.DefaultSelectedRange);
        return properties;
    }

    [PageCommand(CommandName = "LOADDATA")]
    public async Task<ICommandResponse> PageCommandHandler(int fromMonths)
    {
        var stats = await GetStats(fromMonths);
        return ResponseFrom(stats);
    }

    private async Task<StatsData> GetStats(int fromMonths)
    {
        var startDate = clock.UtcNow.AddMonths(-fromMonths);
        var totals = await GetTotals(startDate);
        var data = await GetActivities(startDate, fromMonths);

        return new StatsData(totals, DateOnly.FromDateTime(startDate), data);
    }

    private async Task<ImmutableList<StatsDatum>> GetActivities(DateTime startDate, int fromMonths)
    {
        string query = $"""
        SELECT 
            FORMAT([ActivityCreated], 'yyyy') AS Year,
            FORMAT([ActivityCreated], 'MM') AS Month,
            COALESCE(ActivityTypeDisplayName, ActivityType) as ActivityName,
            COUNT(ActivityID) AS Count
        FROM OM_Activity
        LEFT OUTER JOIN OM_ActivityType
            ON ActivityType = ActivityTypeName
        WHERE 
            [ActivityCreated] >= @StartDate
        GROUP BY 
            FORMAT([ActivityCreated], 'yyyy'),
            FORMAT([ActivityCreated], 'MM'),
            COALESCE(ActivityTypeDisplayName, ActivityType)
        ORDER BY 
            ActivityName DESC,
            Year ASC,
            Month ASC;
        """;

        var qp = new QueryDataParameters
        {
            new DataParameter("@StartDate", startDate)
        };

        var reader = await ConnectionHelper.ExecuteReaderAsync(query, qp, QueryTypeEnum.SQLQuery, CommandBehavior.Default, default);
        var dataLookup = new Dictionary<string, Dictionary<string, int>>();

        while (reader.Read())
        {
            string activity = reader.GetString("ActivityName");
            string year = reader.GetString("Year");
            string month = reader.GetString("Month");
            int count = reader.GetInt32("Count");
            string key = $"{month}/{year}";

            if (!dataLookup.TryGetValue(activity, out var entries))
            {
                entries = GetEntries();
                dataLookup[activity] = entries;
            }

            if (entries.ContainsKey(key))
            {
                entries[key] = count;
            }
        }

        await reader.CloseAsync();

        var timeSeries = dataLookup.ToImmutableList()
            .Select(item =>
            {
                var entries = item.Value
                    .Select(kv => new TimeSeriesEntry(kv.Key, kv.Value))
                    .ToImmutableList();

                return new StatsDatum(item.Key, entries);
            });

        return [.. timeSeries];

        Dictionary<string, int> GetEntries() =>
            Enumerable.Range(1, fromMonths)
            .Select(i => startDate.AddMonths(i).ToString("MM/yyyy"))
            .ToDictionary(k => k, v => 0);
    }

    private static async Task<ImmutableList<StatsTotal>> GetTotals(DateTime startDate)
    {
        string query = $"""
        SELECT 
            ActivityTypeDisplayName as ActivityName,
            COUNT(ActivityID) as Count
        FROM OM_ActivityType 
        LEFT OUTER JOIN OM_Activity
            ON ActivityTypeName = ActivityType AND [ActivityCreated] >= @StartDate            
        GROUP BY ActivityTypeDisplayName
        ORDER BY ActivityName DESC
        """;

        var qp = new QueryDataParameters
        {
            new DataParameter("@StartDate", startDate)
        };

        var reader = await ConnectionHelper.ExecuteReaderAsync(query, qp, QueryTypeEnum.SQLQuery, CommandBehavior.Default, default);
        var totals = new List<StatsTotal>();

        while (reader.Read())
        {
            string label = reader.GetString("ActivityName");
            int count = reader.GetInt32("Count");

            totals.Add(new(label, count));
        }

        await reader.CloseAsync();

        return [.. totals];
    }
}
