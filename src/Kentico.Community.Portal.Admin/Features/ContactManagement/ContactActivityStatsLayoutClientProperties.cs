using System.Collections.Immutable;
using Kentico.Xperience.Admin.Base;

namespace Kentico.Community.Portal.Admin.Features.ContactManagement;

public class ContactActivityStatsLayoutClientProperties : TemplateClientProperties
{
    public ContactActivityStatsDashboard Stats { get; set; } = null!;
    public string Title { get; set; } = "Recent contact activity";
    public string Description { get; set; } = "Track top-of-funnel contact behavior in the recent window where anonymous engagement is still actionable.";
}

public record ContactActivityStatsQuery(DateTime RangeStart, DateTime RangeEnd, string? FocusActivityTypeKey);

public record ContactActivityStatsDashboard(
    DateOnly RangeStartDate,
    DateOnly RangeEndDate,
    string FocusActivityTypeKey,
    ContactActivityOverview Overview,
    ImmutableList<ContactActivityTypeSummary> ActivityTypes,
    ImmutableList<ContactActivitySeries> Series,
    ImmutableList<ContactActivitySignal> Signals);

public record ContactActivityOverview(
    int TotalActivities,
    int ActiveContacts,
    int RepeatContacts,
    decimal RepeatContactRate,
    int AverageActivitiesPerContact,
    int ActivityTypesTracked,
    int ActiveDays,
    string LeadingActivityLabel,
    int LeadingActivityValue,
    string PeakMonthLabel,
    int PeakMonthValue);

public record ContactActivityTypeSummary(
    string Key,
    string Label,
    string Description,
    int RangeTotal,
    int ActiveContacts,
    int ActiveMonths,
    string PeakMonthLabel,
    int PeakMonthValue,
    int LatestMonthValue,
    int PreviousMonthValue,
    decimal SharePercent,
    decimal? MonthOverMonthChangePercent);

public record ContactActivitySeries(string Key, string Label, ImmutableList<ContactActivityPoint> Points);

public record ContactActivityPoint(DateOnly PeriodStart, string Label, int Value);

public record ContactActivitySignal(string Key, string Label, string Value, string Context);
