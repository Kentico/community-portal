using System.Collections.Immutable;
using Kentico.Xperience.Admin.Base;

namespace Kentico.Community.Portal.Admin.Features.ContactManagement;

public class ContactActivityPathLayoutClientProperties : TemplateClientProperties
{
    public ContactActivityPathDashboard Stats { get; set; } = null!;
    public string Title { get; set; } = "Contact activity path";
    public string Description { get; set; } = "Review the selected contact's recorded activities as a time-ordered path within the selected range.";
}

public record ContactActivityPathQuery(DateTime RangeStart, DateTime RangeEnd);

public record ContactActivityPathDashboard(
    DateOnly RangeStartDate,
    DateOnly RangeEndDate,
    ContactActivityPathContact Contact,
    ContactActivityPathOverview Overview,
    ImmutableList<ContactActivityPathItem> Items);

public record ContactActivityPathContact(string DisplayName, string Email);

public record ContactActivityPathOverview(
    int TotalActivities,
    int ActivityTypesTracked,
    int ActiveDays,
    string FirstActivityAt,
    string LastActivityAt);

public record ContactActivityPathItem(
    string Key,
    string ActivityTypeKey,
    string ActivityTypeLabel,
    string DayLabel,
    DateTime StartTime,
    DateTime EndTime,
    string OccurredAtLabel,
    string Detail);
