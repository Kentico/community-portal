using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using CMS.DataEngine;
using Kentico.Community.Portal.Admin.Features.Reporting;
using Kentico.Community.Portal.Admin.Features.Stats;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    uiPageType: typeof(CommunityStatsPage),
    parentType: typeof(ReportingApplicationPage),
    slug: "community-stats",
    name: "Community",
    templateName: "@kentico-community/portal-web-admin/CommunityStatsLayout",
    order: 1,
    Icon = Icons.Graph)]

namespace Kentico.Community.Portal.Admin.Features.Stats;

public class CommunityStatsPage(TimeProvider clock) : Page<CommunityStatsLayoutClientProperties>
{
    public const string IDENTIFIER = "community-stats";
    private static readonly ImmutableArray<MetricDefinition> metricDefinitions =
    [
        new(
            "members",
            "New active members",
            "Enabled member accounts created during the selected period.",
            "[MemberCreated]",
            "CMS_Member",
            "MemberEnabled = 1"),
        new(
            "subscribers",
            "Newsletter subscribers",
            "Confirmed recipient-list subscriptions added during the selected period.",
            "ESC.[EmailSubscriptionConfirmationDate]",
            "OM_ContactGroupMember CGM\nINNER JOIN EmailLibrary_EmailSubscriptionConfirmation ESC\n    ON CGM.ContactGroupMemberRelatedID = ESC.EmailSubscriptionConfirmationContactID\nINNER JOIN OM_ContactGroup CG\n    ON CGM.ContactGroupMemberContactGroupID = CG.ContactGroupID",
            "ESC.EmailSubscriptionConfirmationIsApproved = 1 AND CG.ContactGroupIsRecipientList = 1"),
        new(
            "blog-posts",
            "Blog posts",
            "Published blog posts created during the selected period.",
            "[BlogPostPagePublishedDate]",
            "KenticoCommunity_BlogPostPage",
            "1 = 1"),
        new(
            "questions",
            "Q&A questions",
            "Questions opened by the community during the selected period.",
            "[QAndAQuestionPageDateCreated]",
            "KenticoCommunity_QAndAQuestionPage",
            "1 = 1"),
        new(
            "answers",
            "Q&A answers",
            "Answers published in community discussions during the selected period.",
            "[QAndAAnswerDataDateCreated]",
            "KenticoCommunity_QAndAAnswerData",
            "1 = 1"),
    ];

    private readonly TimeProvider clock = clock;

    public override async Task<CommunityStatsLayoutClientProperties> ConfigureTemplateProperties(CommunityStatsLayoutClientProperties properties)
    {
        var defaultWindow = GetDefaultWindow();
        properties.Stats = await GetDashboard(
            new(defaultWindow.StartDate, defaultWindow.EndDateExclusive.AddDays(-1), properties.DefaultFocusedMetricKey),
            CancellationToken.None);

        return properties;
    }

    [PageCommand(CommandName = "LOADDATA")]
    public async Task<ICommandResponse> PageCommandHandler(CommunityStatsQuery query, CancellationToken cancellationToken)
    {
        var stats = await GetDashboard(query, cancellationToken);
        return ResponseFrom(stats);
    }

    private async Task<CommunityStatsDashboard> GetDashboard(CommunityStatsQuery query, CancellationToken cancellationToken)
    {
        var focusedMetric = ResolveMetric(query.FocusMetricKey);
        var window = GetWindow(query.RangeStart, query.RangeEnd);

        var summaries = new List<CommunityMetricSummary>(metricDefinitions.Length);
        var series = new List<CommunityMetricSeries>(metricDefinitions.Length);

        foreach (var metric in metricDefinitions)
        {
            var metricResult = await BuildMetricResult(metric, window, cancellationToken);
            summaries.Add(metricResult.Summary);
            series.Add(metricResult.Series);
        }

        var selectedSummary = summaries.First(item => item.Key == focusedMetric.Key);
        var selectedSeries = series.First(item => item.Key == focusedMetric.Key);
        var focusDetail = BuildFocusDetail(focusedMetric, selectedSummary, selectedSeries.Points);
        var overview = BuildOverview(summaries, series);
        var supplementalSummary = await GetSupplementalSignalSummary(window, cancellationToken);
        var contributorActivity = await GetContributorActivity(window, cancellationToken);
        var composition = BuildActivityMix(summaries, supplementalSummary.TotalReactions);
        var supplementalSignals = BuildSupplementalSignals(supplementalSummary, contributorActivity);

        return new(
            DateOnly.FromDateTime(window.StartDate),
            DateOnly.FromDateTime(window.EndDateExclusive.AddDays(-1)),
            focusedMetric.Key,
            overview,
            [.. summaries],
            [.. series],
            composition,
            supplementalSignals,
            focusDetail);
    }

    private async Task<MetricResult> BuildMetricResult(
        MetricDefinition metric,
        ReportingWindow window,
        CancellationToken cancellationToken)
    {
        var points = await GetTimeSeries(metric, window, cancellationToken);
        var counts = await GetMetricCounts(metric, window, cancellationToken);
        int latestMonthValue = points[^1].Value;
        int previousMonthValue = points.Count > 1 ? points[^2].Value : 0;
        int changeValue = latestMonthValue - previousMonthValue;
        int rangeTotal = points.Sum(item => item.Value);

        return new(
            new(
                metric.Key,
                metric.Label,
                metric.Description,
                rangeTotal,
                counts.AllTimeTotal,
                counts.PreviousRangeTotal,
                GetPercentChange(rangeTotal, counts.PreviousRangeTotal),
                latestMonthValue,
                previousMonthValue,
                changeValue,
                GetPercentChange(latestMonthValue, previousMonthValue)),
            new(metric.Key, metric.Label, points));
    }

    private static CommunityMetricDetail BuildFocusDetail(
        MetricDefinition metric,
        CommunityMetricSummary summary,
        ImmutableList<CommunityMetricPoint> points)
    {
        int averageMonthlyValue = (int)Math.Round(points.Average(item => item.Value), MidpointRounding.AwayFromZero);
        var peakMonth = points
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.PeriodStart)
            .First();

        return new(
            metric.Key,
            metric.Label,
            metric.Description,
            summary.RangeTotal,
            summary.PreviousRangeTotal,
            summary.RangeChangePercent,
            averageMonthlyValue,
            peakMonth,
            points,
            BuildMovingAverage(points, 3));
    }

    private static CommunityStatsOverview BuildOverview(
        IReadOnlyCollection<CommunityMetricSummary> summaries,
        IReadOnlyCollection<CommunityMetricSeries> series)
    {
        var leadingMetric = summaries
            .OrderByDescending(item => item.RangeTotal)
            .ThenBy(item => item.Label)
            .First();

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
            .First();

        return new(
            summaries.Sum(item => item.RangeTotal),
            summaries.Sum(item => item.AllTimeTotal),
            leadingMetric.Label,
            leadingMetric.RangeTotal,
            peakMonth.PeriodStart.ToString("MMM yyyy", CultureInfo.InvariantCulture),
            peakMonth.Total);
    }

    private async Task<ImmutableList<CommunityMetricPoint>> GetTimeSeries(
        MetricDefinition metric,
        ReportingWindow window,
        CancellationToken cancellationToken)
    {
        string query = $"""
        SELECT
            YEAR({metric.DateColumn}) AS [Year],
            MONTH({metric.DateColumn}) AS [Month],
            COUNT(*) AS Count
        FROM {metric.Source}
        WHERE {BuildWhereClause(metric, includeStartDate: true, includeEndDate: true)}
        GROUP BY
            YEAR({metric.DateColumn}),
            MONTH({metric.DateColumn})
        ORDER BY
            [Year] ASC,
            [Month] ASC;
        """;

        var parameters = CreateParameters(window.StartDate, window.EndDateExclusive);
        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);
        var countsByMonth = new Dictionary<DateOnly, int>();

        while (reader.Read())
        {
            int year = reader.GetInt32("Year");
            int month = reader.GetInt32("Month");
            int count = reader.GetInt32("Count");
            countsByMonth[new DateOnly(year, month, 1)] = count;
        }

        return
        [
            .. Enumerable.Range(0, window.BucketCount)
                .Select(offset =>
                {
                    var periodStart = window.BucketStartDate.AddMonths(offset);
                    var period = DateOnly.FromDateTime(periodStart);

                    return new CommunityMetricPoint(
                        period,
                        periodStart.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                        countsByMonth.GetValueOrDefault(period));
                })
        ];
    }

    private async Task<MetricCountResult> GetMetricCounts(
        MetricDefinition metric,
        ReportingWindow window,
        CancellationToken cancellationToken)
    {
        string query = $"""
        SELECT
            COUNT(*) AS [AllTimeTotal],
                COALESCE(SUM(CASE
                    WHEN {metric.DateColumn} >= @PreviousStartDate AND {metric.DateColumn} < @PreviousEndDate
                        THEN 1
                    ELSE 0
                END), 0) AS [PreviousRangeTotal]
        FROM {metric.Source}
        WHERE {metric.Predicate}
        """;

        var parameters = new QueryDataParameters
        {
            new DataParameter("@PreviousStartDate", window.PreviousStartDate),
            new DataParameter("@PreviousEndDate", window.PreviousEndDateExclusive),
        };

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);
        int allTimeTotal = 0;
        int previousRangeTotal = 0;

        while (reader.Read())
        {
            allTimeTotal = reader.GetInt32("AllTimeTotal");
            previousRangeTotal = reader.GetInt32("PreviousRangeTotal");
        }

        return new(allTimeTotal, previousRangeTotal);
    }

    private static ImmutableList<CommunityCompositionSlice> BuildActivityMix(
        IReadOnlyCollection<CommunityMetricSummary> summaries,
        int totalReactions)
    {
        var mix = summaries
            .Where(item => item.RangeTotal > 0)
            .Select(item => new CommunityCompositionSlice(item.Key, item.Label, item.RangeTotal))
            .ToList();

        if (totalReactions > 0)
        {
            mix.Add(new("q-and-a-reactions", "Q&A reactions", totalReactions));
        }

        return [.. mix.OrderByDescending(item => item.Value).ThenBy(item => item.Label)];
    }

    private static ImmutableList<CommunitySupplementalSignal> BuildSupplementalSignals(
        SupplementalSignalSummary summary,
        ContributorActivityResult contributors)
    {
        string averageContributorValue = contributors.AverageMonthlyContributors > 0
            ? $"{contributors.AverageMonthlyContributors:N0}/mo"
            : "0/mo";

        string contributorContext = contributors.PeakMonthCount > 0
            ? $"Peak {contributors.PeakMonthCount:N0} in {contributors.PeakMonthLabel}"
            : "No member-authored Q&A activity in this window";

        string acceptedRateValue = $"{summary.AcceptedAnswerRate:N1}%";
        string acceptedContext = $"{summary.AcceptedAnswers:N0} questions currently have an accepted answer";

        string reactionsValue = summary.TotalReactions.ToString("N0", CultureInfo.InvariantCulture);
        const string reactionsContext = "Q&A question and answer reactions in this window";

        string responseLagValue = summary.AnsweredQuestions > 0
            ? FormatDuration(summary.AverageResponseHours)
            : "n/a";
        string responseLagContext = summary.AnsweredQuestions > 0
            ? $"Average first response across {summary.AnsweredQuestions:N0} answered questions"
            : "No answered questions in this window";

        return
        [
            new("accepted-answers", "Accepted answers", acceptedRateValue, acceptedContext),
            new("contributors", "Unique contributors", averageContributorValue, contributorContext),
            new("reactions", "Q&A reactions", reactionsValue, reactionsContext),
            new("response-lag", "Question-to-response lag", responseLagValue, responseLagContext),
        ];
    }

    private static async Task<SupplementalSignalSummary> GetSupplementalSignalSummary(
        ReportingWindow window,
        CancellationToken cancellationToken)
    {
        string query = """
        WITH [FirstResponses] AS (
            SELECT
                [QAndAAnswerDataQuestionWebPageItemID] AS [WebPageItemID],
                MIN([QAndAAnswerDataDateCreated]) AS [FirstAnswerDate]
            FROM [KenticoCommunity_QAndAAnswerData]
            GROUP BY [QAndAAnswerDataQuestionWebPageItemID]
        )
        SELECT
            COALESCE((
                SELECT SUM(CASE
                    WHEN [QAndAQuestionPageAcceptedAnswerDataGUID] <> @EmptyGuid THEN 1
                    ELSE 0
                END)
                FROM [KenticoCommunity_QAndAQuestionPage]
                WHERE [QAndAQuestionPageDateCreated] >= @StartDate
                    AND [QAndAQuestionPageDateCreated] < @EndDate
            ), 0) AS [AcceptedAnswers],
            COALESCE((
                SELECT CAST(SUM(CASE
                    WHEN [QAndAQuestionPageAcceptedAnswerDataGUID] <> @EmptyGuid THEN 1
                    ELSE 0
                END) AS DECIMAL(18, 2)) * 100.0 / NULLIF(COUNT(*), 0)
                FROM [KenticoCommunity_QAndAQuestionPage]
                WHERE [QAndAQuestionPageDateCreated] >= @StartDate
                    AND [QAndAQuestionPageDateCreated] < @EndDate
            ), 0) AS [AcceptedAnswerRate],
            COALESCE((
                SELECT COUNT(*)
                FROM [KenticoCommunity_QAndAQuestionReaction]
                WHERE [QAndAQuestionReactionDateModified] >= @StartDate
                    AND [QAndAQuestionReactionDateModified] < @EndDate
            ), 0) + COALESCE((
                SELECT COUNT(*)
                FROM [KenticoCommunity_QAndAAnswerReaction]
                WHERE [QAndAAnswerReactionDateModified] >= @StartDate
                    AND [QAndAAnswerReactionDateModified] < @EndDate
            ), 0) AS [TotalReactions],
            COALESCE((
                SELECT AVG(CAST(DATEDIFF(MINUTE, Q.[QAndAQuestionPageDateCreated], FR.[FirstAnswerDate]) AS DECIMAL(18, 2))) / 60.0
                FROM [KenticoCommunity_QAndAQuestionPage] Q
                INNER JOIN [CMS_ContentItemCommonData] CCD
                    ON Q.[ContentItemDataCommonDataID] = CCD.[ContentItemCommonDataID]
                INNER JOIN [CMS_WebPageItem] WPI
                    ON CCD.[ContentItemCommonDataContentItemID] = WPI.[WebPageItemContentItemID]
                INNER JOIN [FirstResponses] FR
                    ON WPI.[WebPageItemID] = FR.[WebPageItemID]
                WHERE Q.[QAndAQuestionPageDateCreated] >= @StartDate
                    AND Q.[QAndAQuestionPageDateCreated] < @EndDate
            ), 0) AS [AverageResponseHours],
            COALESCE((
                SELECT COUNT(*)
                FROM [KenticoCommunity_QAndAQuestionPage] Q
                INNER JOIN [CMS_ContentItemCommonData] CCD
                    ON Q.[ContentItemDataCommonDataID] = CCD.[ContentItemCommonDataID]
                INNER JOIN [CMS_WebPageItem] WPI
                    ON CCD.[ContentItemCommonDataContentItemID] = WPI.[WebPageItemContentItemID]
                INNER JOIN [FirstResponses] FR
                    ON WPI.[WebPageItemID] = FR.[WebPageItemID]
                WHERE Q.[QAndAQuestionPageDateCreated] >= @StartDate
                    AND Q.[QAndAQuestionPageDateCreated] < @EndDate
            ), 0) AS [AnsweredQuestions]
        """;

        var parameters = new QueryDataParameters
        {
            new DataParameter("@StartDate", window.StartDate),
            new DataParameter("@EndDate", window.EndDateExclusive),
            new DataParameter("@EmptyGuid", Guid.Empty),
        };

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);

        if (!reader.Read())
        {
            return new(0, 0, 0, 0, 0);
        }

        return new(
            reader.GetInt32("AcceptedAnswers"),
            reader.GetDecimal("AcceptedAnswerRate"),
            reader.GetInt32("TotalReactions"),
            reader.GetDecimal("AverageResponseHours"),
            reader.GetInt32("AnsweredQuestions"));
    }

    private static async Task<ContributorActivityResult> GetContributorActivity(
        ReportingWindow window,
        CancellationToken cancellationToken)
    {
        string query = """
        WITH [Contributors] AS (
            SELECT
                [QAndAQuestionPageDateCreated] AS [ContributionDate],
                [QAndAQuestionPageAuthorMemberID] AS [MemberID]
            FROM [KenticoCommunity_QAndAQuestionPage]
            WHERE [QAndAQuestionPageDateCreated] >= @StartDate
                AND [QAndAQuestionPageDateCreated] < @EndDate
                AND [QAndAQuestionPageAuthorMemberID] > 0

            UNION ALL

            SELECT
                [QAndAAnswerDataDateCreated] AS [ContributionDate],
                [QAndAAnswerDataAuthorMemberID] AS [MemberID]
            FROM [KenticoCommunity_QAndAAnswerData]
            WHERE [QAndAAnswerDataDateCreated] >= @StartDate
                AND [QAndAAnswerDataDateCreated] < @EndDate
                AND [QAndAAnswerDataAuthorMemberID] > 0
        )
        SELECT
            YEAR([ContributionDate]) AS [Year],
            MONTH([ContributionDate]) AS [Month],
            COUNT(DISTINCT [MemberID]) AS [Count]
        FROM [Contributors]
        GROUP BY YEAR([ContributionDate]), MONTH([ContributionDate])
        ORDER BY [Year], [Month]
        """;

        var parameters = CreateParameters(window.StartDate, window.EndDateExclusive);
        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);
        var countsByMonth = new Dictionary<DateOnly, int>();

        while (reader.Read())
        {
            countsByMonth[new DateOnly(reader.GetInt32("Year"), reader.GetInt32("Month"), 1)] = reader.GetInt32("Count");
        }

        var monthlyContributorCounts = Enumerable.Range(0, window.BucketCount)
            .Select(offset =>
            {
                var periodStart = window.BucketStartDate.AddMonths(offset);
                var period = DateOnly.FromDateTime(periodStart);

                return new CommunityMetricPoint(
                    period,
                    periodStart.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    countsByMonth.GetValueOrDefault(period));
            })
            .ToImmutableList();

        int averageMonthlyContributors = monthlyContributorCounts.Count > 0
            ? (int)Math.Round(monthlyContributorCounts.Average(item => item.Value), MidpointRounding.AwayFromZero)
            : 0;

        var peakMonth = monthlyContributorCounts
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.PeriodStart)
            .FirstOrDefault();

        return new(
            averageMonthlyContributors,
            peakMonth?.Label ?? string.Empty,
            peakMonth?.Value ?? 0);
    }

    private static string FormatDuration(decimal hours)
    {
        if (hours >= 24)
        {
            decimal days = Math.Round(hours / 24, 1, MidpointRounding.AwayFromZero);
            return $"{days:N1}d";
        }

        decimal roundedHours = Math.Round(hours, 1, MidpointRounding.AwayFromZero);
        return $"{roundedHours:N1}h";
    }

    private static ImmutableList<CommunityMetricPoint> BuildMovingAverage(ImmutableList<CommunityMetricPoint> points, int windowSize) => [
            .. points.Select((point, index) =>
            {
                int startIndex = Math.Max(0, index - windowSize + 1);
                var window = points.Skip(startIndex).Take(index - startIndex + 1);
                int average = (int)Math.Round(window.Average(item => item.Value), MidpointRounding.AwayFromZero);

                return point with { Value = average };
            })
        ];

    private ReportingWindow GetDefaultWindow()
    {
        var currentUtc = clock.GetUtcNow().UtcDateTime;
        var currentMonth = new DateTime(currentUtc.Year, currentUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startDate = currentMonth.AddMonths(-11);
        var endDateExclusive = currentMonth.AddMonths(1);

        return new(
            startDate,
            endDateExclusive,
            startDate,
            GetBucketCount(startDate, endDateExclusive),
            startDate.AddMonths(-12),
            startDate);
    }

    private static ReportingWindow GetWindow(DateTime rangeStart, DateTime rangeEnd)
    {
        var normalizedStart = DateTime.SpecifyKind(rangeStart.Date, DateTimeKind.Utc);
        var normalizedEndExclusive = DateTime.SpecifyKind(rangeEnd.Date.AddDays(1), DateTimeKind.Utc);
        var bucketStartDate = new DateTime(normalizedStart.Year, normalizedStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var previousRangeDuration = normalizedEndExclusive - normalizedStart;

        return new(
            normalizedStart,
            normalizedEndExclusive,
            bucketStartDate,
            GetBucketCount(bucketStartDate, normalizedEndExclusive),
            normalizedStart.Subtract(previousRangeDuration),
            normalizedStart);
    }

    private static QueryDataParameters CreateParameters(DateTime startDate, DateTime endDateExclusive) =>
        [
            new DataParameter("@StartDate", startDate),
            new DataParameter("@EndDate", endDateExclusive),
        ];

    private static string BuildWhereClause(MetricDefinition metric, bool includeStartDate, bool includeEndDate)
    {
        var conditions = new List<string> { metric.Predicate };

        if (includeStartDate)
        {
            conditions.Add($"{metric.DateColumn} >= @StartDate");
        }

        if (includeEndDate)
        {
            conditions.Add($"{metric.DateColumn} < @EndDate");
        }

        return string.Join(Environment.NewLine + "    AND ", conditions);
    }

    private static int GetBucketCount(DateTime bucketStartDate, DateTime endDateExclusive)
    {
        var inclusiveEndDate = endDateExclusive.AddDays(-1);
        var endBucketStartDate = new DateTime(inclusiveEndDate.Year, inclusiveEndDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return ((endBucketStartDate.Year - bucketStartDate.Year) * 12) + endBucketStartDate.Month - bucketStartDate.Month + 1;
    }

    private static MetricDefinition ResolveMetric(string? focusMetricKey) =>
        metricDefinitions.FirstOrDefault(item => item.Key.Equals(focusMetricKey, StringComparison.OrdinalIgnoreCase)) ?? metricDefinitions[0];

    private static decimal? GetPercentChange(int currentValue, int previousValue)
    {
        if (previousValue == 0)
        {
            return currentValue == 0 ? 0 : null;
        }

        decimal change = (decimal)(currentValue - previousValue) / previousValue * 100;
        return Math.Round(change, 1, MidpointRounding.AwayFromZero);
    }

    private readonly record struct ReportingWindow(
        DateTime StartDate,
        DateTime EndDateExclusive,
        DateTime BucketStartDate,
        int BucketCount,
        DateTime PreviousStartDate,
        DateTime PreviousEndDateExclusive);

    private sealed record MetricDefinition(
        string Key,
        string Label,
        string Description,
        string DateColumn,
        string Source,
        string Predicate);

    private sealed record MetricResult(
        CommunityMetricSummary Summary,
        CommunityMetricSeries Series);

    private sealed record MetricCountResult(
        int AllTimeTotal,
        int PreviousRangeTotal);

    private sealed record SupplementalSignalSummary(
        int AcceptedAnswers,
        decimal AcceptedAnswerRate,
        int TotalReactions,
        decimal AverageResponseHours,
        int AnsweredQuestions);

    private sealed record ContributorActivityResult(
        int AverageMonthlyContributors,
        string PeakMonthLabel,
        int PeakMonthCount);
}
