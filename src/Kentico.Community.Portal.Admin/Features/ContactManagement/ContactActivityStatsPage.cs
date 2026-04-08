using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using CMS.Activities;
using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.ContactManagement;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: UIPage(
    uiPageType: typeof(ContactActivityStatsPage),
    parentType: typeof(ContactManagementApplication),
    slug: "reporting",
    name: "Reporting",
    templateName: "@kentico-community/portal-web-admin/ContactActivityStatsLayout",
    order: 1000,
    Icon = Icons.Graph)]

namespace Kentico.Community.Portal.Admin.Features.ContactManagement;

public class ContactActivityStatsPage(TimeProvider clock, IInfoProvider<ActivityTypeInfo> activityTypeProvider) : Page<ContactActivityStatsLayoutClientProperties>
{
    private const int DefaultWindowMonths = 6;
    public const string IDENTIFIER = "contact-activity-stats";

    private readonly TimeProvider clock = clock;
    private readonly IInfoProvider<ActivityTypeInfo> activityTypeProvider = activityTypeProvider;

    public override async Task<ContactActivityStatsLayoutClientProperties> ConfigureTemplateProperties(ContactActivityStatsLayoutClientProperties properties)
    {
        var defaultWindow = GetDefaultWindow();
        properties.Stats = await GetDashboard(
            new(defaultWindow.StartDate, defaultWindow.EndDateExclusive.AddDays(-1), null),
            CancellationToken.None);

        return properties;
    }

    [PageCommand(CommandName = "LOADDATA")]
    public async Task<ICommandResponse> PageCommandHandler(ContactActivityStatsQuery query, CancellationToken cancellationToken)
    {
        var stats = await GetDashboard(query, cancellationToken);
        return ResponseFrom(stats);
    }

    private async Task<ContactActivityStatsDashboard> GetDashboard(ContactActivityStatsQuery query, CancellationToken cancellationToken)
    {
        var window = GetWindow(query.RangeStart, query.RangeEnd);
        var activityTypeMetadata = await GetActivityTypeMetadata(cancellationToken);
        var monthlyActivity = await GetMonthlyActivity(window, cancellationToken);
        var rangeCounts = await GetRangeCounts(window, cancellationToken);
        var contactsByType = await GetTypeContactCounts(window, cancellationToken);
        var series = BuildSeries(monthlyActivity, window, activityTypeMetadata);
        var activityTypes = BuildActivityTypes(series, contactsByType, rangeCounts.TotalActivities, activityTypeMetadata);
        string focusActivityTypeKey = ResolveFocusActivityTypeKey(query.FocusActivityTypeKey, activityTypes);
        var overview = BuildOverview(rangeCounts, activityTypes, series);
        var signals = BuildSignals(overview, window, activityTypes);

        return new(
            DateOnly.FromDateTime(window.StartDate),
            DateOnly.FromDateTime(window.EndDateExclusive.AddDays(-1)),
            focusActivityTypeKey,
            overview,
            activityTypes,
            series,
            signals);
    }

    private static ContactActivityOverview BuildOverview(
        RangeCountsResult rangeCounts,
        IReadOnlyList<ContactActivityTypeSummary> activityTypes,
        IReadOnlyList<ContactActivitySeries> series)
    {
        var leadingActivity = activityTypes.FirstOrDefault();
        var peakMonth = series
            .SelectMany(item => item.Points)
            .GroupBy(item => item.PeriodStart)
            .Select(group => new
            {
                PeriodStart = group.Key,
                Total = group.Sum(item => item.Value),
            })
            .OrderByDescending(item => item.Total)
            .ThenBy(item => item.PeriodStart)
            .FirstOrDefault();

        decimal repeatContactRate = rangeCounts.ActiveContacts > 0
            ? decimal.Round((decimal)rangeCounts.RepeatContacts * 100 / rangeCounts.ActiveContacts, 1)
            : 0;

        int averageActivitiesPerContact = rangeCounts.ActiveContacts > 0
            ? (int)Math.Round((decimal)rangeCounts.TotalActivities / rangeCounts.ActiveContacts, MidpointRounding.AwayFromZero)
            : 0;

        return new(
            rangeCounts.TotalActivities,
            rangeCounts.ActiveContacts,
            rangeCounts.RepeatContacts,
            repeatContactRate,
            averageActivitiesPerContact,
            activityTypes.Count,
            rangeCounts.ActiveDays,
            leadingActivity?.Label ?? "No tracked activity",
            leadingActivity?.RangeTotal ?? 0,
            peakMonth?.PeriodStart.ToString("MMM yyyy", CultureInfo.InvariantCulture) ?? "No recent activity",
            peakMonth?.Total ?? 0);
    }

    private static ImmutableList<ContactActivitySignal> BuildSignals(
        ContactActivityOverview overview,
        ContactReportingWindow window,
        IReadOnlyList<ContactActivityTypeSummary> activityTypes)
    {
        string windowMonths = window.BucketCount == 1 ? "1 month" : $"{window.BucketCount} months";
        string leadingContext = activityTypes.Count > 1
            ? $"Next signal: {activityTypes[1].Label} with {activityTypes[1].RangeTotal:N0} activities"
            : "Only one activity type is active in the selected window";

        return
        [
            new(
                "retention-window",
                "Retention horizon",
                "12-month cleanup",
                "Inactive anonymous contacts can age out automatically, so this dashboard defaults to a recent operating window."),
            new(
                "repeat-contacts",
                "Repeat contact share",
                $"{overview.RepeatContactRate:N1}%",
                $"{overview.RepeatContacts:N0} of {overview.ActiveContacts:N0} active contacts generated more than one tracked activity in {windowMonths}."),
            new(
                "avg-intensity",
                "Average activity intensity",
                overview.AverageActivitiesPerContact.ToString("N0", CultureInfo.InvariantCulture),
                "Average tracked activities per active contact across the selected range."),
            new(
                "leading-activity",
                "Leading signal",
                overview.LeadingActivityLabel,
                leadingContext),
        ];
    }

    private static ImmutableList<ContactActivityTypeSummary> BuildActivityTypes(
        IReadOnlyList<ContactActivitySeries> series,
        IReadOnlyDictionary<string, int> contactsByType,
        int totalActivities,
        IReadOnlyDictionary<string, ContactActivityTypeMetadata> activityTypeMetadata) =>
        [
            .. series
                .Select(item =>
                {
                    var metadata = activityTypeMetadata.GetValueOrDefault(item.Key) ?? CreateFallbackMetadata(item.Key);
                    var peakMonth = item.Points
                        .OrderByDescending(point => point.Value)
                        .ThenBy(point => point.PeriodStart)
                        .FirstOrDefault();

                    int rangeTotal = item.Points.Sum(point => point.Value);
                    int latestMonthValue = item.Points.LastOrDefault()?.Value ?? 0;
                    int previousMonthValue = item.Points.Count > 1 ? item.Points[^2].Value : 0;
                    decimal sharePercent = totalActivities > 0
                        ? decimal.Round((decimal)rangeTotal * 100 / totalActivities, 1)
                        : 0;

                    decimal? monthOverMonthChangePercent = previousMonthValue > 0
                        ? decimal.Round((decimal)(latestMonthValue - previousMonthValue) * 100 / previousMonthValue, 1)
                        : null;

                    return new ContactActivityTypeSummary(
                        item.Key,
                        item.Label,
                        metadata.Description,
                        rangeTotal,
                        contactsByType.GetValueOrDefault(item.Key),
                        item.Points.Count(point => point.Value > 0),
                        peakMonth?.Label ?? string.Empty,
                        peakMonth?.Value ?? 0,
                        latestMonthValue,
                        previousMonthValue,
                        sharePercent,
                        monthOverMonthChangePercent);
                })
                .OrderByDescending(item => item.RangeTotal)
                .ThenBy(item => item.Label)
        ];

    private static ImmutableList<ContactActivitySeries> BuildSeries(
        IReadOnlyList<MonthlyActivityRow> rows,
        ContactReportingWindow window,
        IReadOnlyDictionary<string, ContactActivityTypeMetadata> activityTypeMetadata)
    {
        var rowsByKey = rows
            .GroupBy(item => item.ActivityTypeKey)
            .ToDictionary(group => group.Key, group => group.ToImmutableList(), StringComparer.OrdinalIgnoreCase);

        return
        [
            .. rowsByKey
                .Values
                .Select(group =>
                {
                    var metadata = activityTypeMetadata.GetValueOrDefault(group[0].ActivityTypeKey) ?? CreateFallbackMetadata(group[0].ActivityTypeKey);
                    var countsByPeriod = group.ToDictionary(
                        item => new DateOnly(item.Year, item.Month, 1),
                        item => item.Count);

                    return new ContactActivitySeries(
                        group[0].ActivityTypeKey,
                        metadata.Label,
                        [
                            .. Enumerable.Range(0, window.BucketCount)
                                .Select(offset =>
                                {
                                    var bucketStart = window.BucketStartDate.AddMonths(offset);
                                    var periodStart = DateOnly.FromDateTime(bucketStart);

                                    return new ContactActivityPoint(
                                        periodStart,
                                        bucketStart.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                                        countsByPeriod.GetValueOrDefault(periodStart));
                                })
                        ]);
                })
                .OrderByDescending(item => item.Points.Sum(point => point.Value))
                .ThenBy(item => item.Label)
        ];
    }

    private static string ResolveFocusActivityTypeKey(string? requestedKey, IReadOnlyList<ContactActivityTypeSummary> activityTypes)
    {
        if (!string.IsNullOrWhiteSpace(requestedKey)
            && activityTypes.Any(item => string.Equals(item.Key, requestedKey, StringComparison.OrdinalIgnoreCase)))
        {
            return requestedKey;
        }

        return activityTypes.FirstOrDefault()?.Key ?? string.Empty;
    }

    private static QueryDataParameters CreateRangeParameters(ContactReportingWindow window) =>
        [
            new DataParameter("@StartDate", window.StartDate),
            new DataParameter("@EndDateExclusive", window.EndDateExclusive),
        ];

    private static ContactReportingWindow GetWindow(DateTime rangeStart, DateTime rangeEnd)
    {
        var normalizedStart = rangeStart.Date;
        var normalizedEnd = rangeEnd.Date;

        if (normalizedEnd < normalizedStart)
        {
            (normalizedStart, normalizedEnd) = (normalizedEnd, normalizedStart);
        }

        var bucketStartDate = new DateTime(normalizedStart.Year, normalizedStart.Month, 1);
        var bucketEndDate = new DateTime(normalizedEnd.Year, normalizedEnd.Month, 1);
        int bucketCount = ((bucketEndDate.Year - bucketStartDate.Year) * 12) + bucketEndDate.Month - bucketStartDate.Month + 1;

        return new(normalizedStart, normalizedEnd.AddDays(1), bucketStartDate, bucketCount);
    }

    private ContactReportingWindow GetDefaultWindow()
    {
        var today = clock.GetUtcNow().UtcDateTime.Date;
        var startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-(DefaultWindowMonths - 1));
        return GetWindow(startDate, today);
    }

    private static async Task<RangeCountsResult> GetRangeCounts(ContactReportingWindow window, CancellationToken cancellationToken)
    {
        const string query = """
        WITH [WindowedActivity] AS (
            SELECT
                [ActivityID],
                [ActivityContactID],
                CAST([ActivityCreated] AS date) AS [ActivityDate]
            FROM [OM_Activity]
            WHERE [ActivityCreated] >= @StartDate
                AND [ActivityCreated] < @EndDateExclusive
        ),
        [RepeatContacts] AS (
            SELECT [ActivityContactID]
            FROM [WindowedActivity]
            WHERE [ActivityContactID] IS NOT NULL
            GROUP BY [ActivityContactID]
            HAVING COUNT(*) > 1
        )
        SELECT
            COUNT(*) AS [TotalActivities],
            COUNT(DISTINCT [ActivityContactID]) AS [ActiveContacts],
            COUNT(DISTINCT [ActivityDate]) AS [ActiveDays],
            COALESCE((SELECT COUNT(*) FROM [RepeatContacts]), 0) AS [RepeatContacts]
        FROM [WindowedActivity]
        """;

        var parameters = CreateRangeParameters(window);
        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);

        if (!reader.Read())
        {
            return new(0, 0, 0, 0);
        }

        return new(
            reader.GetInt32("TotalActivities"),
            reader.GetInt32("ActiveContacts"),
            reader.GetInt32("ActiveDays"),
            reader.GetInt32("RepeatContacts"));
    }

    private static async Task<IReadOnlyDictionary<string, int>> GetTypeContactCounts(ContactReportingWindow window, CancellationToken cancellationToken)
    {
        const string query = """
        SELECT
            COALESCE(NULLIF(A.[ActivityType], ''), 'unknown-activity') AS [ActivityTypeKey],
            COUNT(DISTINCT A.[ActivityContactID]) AS [ActiveContacts]
        FROM [OM_Activity] A
        WHERE A.[ActivityCreated] >= @StartDate
            AND A.[ActivityCreated] < @EndDateExclusive
        GROUP BY COALESCE(NULLIF(A.[ActivityType], ''), 'unknown-activity')
        """;

        var parameters = CreateRangeParameters(window);
        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);
        var results = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            results[reader.GetString("ActivityTypeKey")] = reader.GetInt32("ActiveContacts");
        }

        return results;
    }

    private static async Task<ImmutableList<MonthlyActivityRow>> GetMonthlyActivity(ContactReportingWindow window, CancellationToken cancellationToken)
    {
        const string query = """
        SELECT
            YEAR(A.[ActivityCreated]) AS [Year],
            MONTH(A.[ActivityCreated]) AS [Month],
            COALESCE(NULLIF(A.[ActivityType], ''), 'unknown-activity') AS [ActivityTypeKey],
            COUNT(*) AS [Count]
        FROM [OM_Activity] A
        WHERE A.[ActivityCreated] >= @StartDate
            AND A.[ActivityCreated] < @EndDateExclusive
        GROUP BY
            YEAR(A.[ActivityCreated]),
            MONTH(A.[ActivityCreated]),
            COALESCE(NULLIF(A.[ActivityType], ''), 'unknown-activity')
        ORDER BY [Count] DESC, [ActivityTypeKey] ASC, [Year] ASC, [Month] ASC
        """;

        var parameters = CreateRangeParameters(window);
        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);
        var rows = new List<MonthlyActivityRow>();

        while (reader.Read())
        {
            rows.Add(new(
                reader.GetInt32("Year"),
                reader.GetInt32("Month"),
                reader.GetString("ActivityTypeKey"),
                reader.GetInt32("Count")));
        }

        return [.. rows];
    }

    private async Task<IReadOnlyDictionary<string, ContactActivityTypeMetadata>> GetActivityTypeMetadata(CancellationToken cancellationToken)
    {
        var activityTypes = await activityTypeProvider
            .Get()
            .WhereTrue(nameof(ActivityTypeInfo.ActivityTypeEnabled))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return activityTypes.ToDictionary(
            item => item.ActivityTypeName,
            item => new ContactActivityTypeMetadata(
                string.IsNullOrWhiteSpace(item.ActivityTypeDisplayName) ? item.ActivityTypeName : item.ActivityTypeDisplayName,
                !string.IsNullOrWhiteSpace(item.ActivityTypeDescription)
                    ? item.ActivityTypeDescription
                    : GetDefaultActivityTypeDescription(item.ActivityTypeName, item.ActivityTypeDisplayName),
                item.ActivityTypeIsCustom),
            StringComparer.OrdinalIgnoreCase);
    }

    private static ContactActivityTypeMetadata CreateFallbackMetadata(string activityTypeKey) =>
        new(
            ToTitleCase(activityTypeKey),
            GetDefaultActivityTypeDescription(activityTypeKey, activityTypeKey),
            false);

    private static string GetDefaultActivityTypeDescription(string activityTypeName, string displayName) =>
        activityTypeName.ToLowerInvariant() switch
        {
            "pagevisit" => "Tracks page-view volume from recent contacts and is usually the clearest baseline signal for anonymous browsing behavior.",
            "landingpage" => "Shows how often contacts arrive on designated landing pages, which helps isolate campaign and acquisition entry points.",
            "click" => "Captures tracked click interactions and helps show whether visitors are moving past passive browsing into navigation or calls to action.",
            "emailclick" => "Highlights clicks coming from tracked emails so you can separate email-driven sessions from other anonymous web traffic.",
            "bizformsubmit" => "Shows form submission activity, which is one of the strongest anonymous-to-known conversion signals in the contact timeline.",
            "memberregistration" => "Tracks when anonymous contact activity converts into a known member registration event.",
            "datainput" => "Captures meaningful field-entry interactions before a submission is completed, useful for spotting intent even when conversion is unfinished.",
            "chat" => "Represents chat interactions, typically a higher-intent behavior than passive page consumption.",
            "customactivity" => "Custom activity type defined for this application, useful when the standard Kentico activity set does not capture the behavior you need.",
            "unknown-activity" => "Activity records with no resolved type metadata. These should generally be treated as uncategorized tracking signals.",
            _ => $"Recent contact activity for {displayName}."
        };

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown activity";
        }

        string normalized = value.Replace('-', ' ').Replace('_', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
    }

    private sealed record ContactReportingWindow(DateTime StartDate, DateTime EndDateExclusive, DateTime BucketStartDate, int BucketCount);
    private sealed record MonthlyActivityRow(int Year, int Month, string ActivityTypeKey, int Count);
    private sealed record ContactActivityTypeMetadata(string Label, string Description, bool IsCustom);
    private sealed record RangeCountsResult(int TotalActivities, int ActiveContacts, int ActiveDays, int RepeatContacts);
}
