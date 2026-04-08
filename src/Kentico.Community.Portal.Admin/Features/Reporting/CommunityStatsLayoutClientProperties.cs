using System.Collections.Immutable;
using Kentico.Xperience.Admin.Base;

namespace Kentico.Community.Portal.Admin.Features.Reporting;

public class CommunityStatsLayoutClientProperties : TemplateClientProperties
{
    public CommunityStatsDashboard Stats { get; set; } = null!;
    public string DefaultFocusedMetricKey { get; set; } = "members";
    public string Title { get; set; } = "Community growth";
    public string Description { get; set; } = "Track members, subscribers, and publishing activity across the portal.";
}

public record CommunityStatsQuery(DateTime RangeStart, DateTime RangeEnd, string? FocusMetricKey);

public record CommunityStatsDashboard(
    DateOnly RangeStartDate,
    DateOnly RangeEndDate,
    string FocusMetricKey,
    CommunityStatsOverview Overview,
    ImmutableList<CommunityMetricSummary> Highlights,
    ImmutableList<CommunityMetricSeries> Series,
    ImmutableList<CommunityCompositionSlice> Composition,
    ImmutableList<CommunitySupplementalSignal> SupplementalSignals,
    CommunityMetricDetail FocusDetail);

public record CommunityStatsOverview(
    int NewItemsInRange,
    int TrackedRecords,
    string LeadingMetricLabel,
    int LeadingMetricValue,
    string PeakMonthLabel,
    int PeakMonthTotal);

public record CommunityMetricSummary(
    string Key,
    string Label,
    string Description,
    int RangeTotal,
    int AllTimeTotal,
    int PreviousRangeTotal,
    decimal? RangeChangePercent,
    int LatestMonthValue,
    int PreviousMonthValue,
    int ChangeValue,
    decimal? ChangePercent);

public record CommunityMetricSeries(string Key, string Label, ImmutableList<CommunityMetricPoint> Points);

public record CommunityMetricDetail(
    string Key,
    string Label,
    string Description,
    int RangeTotal,
    int PreviousRangeTotal,
    decimal? ChangePercent,
    int AverageMonthlyValue,
    CommunityMetricPoint PeakMonth,
    ImmutableList<CommunityMetricPoint> Points,
    ImmutableList<CommunityMetricPoint> MovingAveragePoints);

public record CommunityCompositionSlice(string Key, string Label, int Value);

public record CommunitySupplementalSignal(string Key, string Label, string Value, string Context);

public record CommunityMetricPoint(DateOnly PeriodStart, string Label, int Value);
