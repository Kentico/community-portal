using System.Collections.Immutable;
using Kentico.Xperience.Admin.Base;

namespace Kentico.Community.Portal.Admin.Features.Reporting;

public class StatsLayoutClientProperties : TemplateClientProperties
{
    public StatsData Stats { get; set; } = null!;
    public IEnumerable<int> AllowedSelectRange { get; set; } = Enumerable.Range(2, 11).Reverse();
    public int DefaultSelectedRange { get; set; } = 12;
    public string TotalsTitle { get; set; } = "Totals";
}

public record StatsData(ImmutableList<StatsTotal> Totals, DateOnly? TotalsStartDate, ImmutableList<StatsDatum> Data);
public record StatsTotal(string Label, int Value);
public record StatsDatum(string Label, ImmutableList<TimeSeriesEntry> DataEntries);
public record TimeSeriesEntry(string Label, int Value);
