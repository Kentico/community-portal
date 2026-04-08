using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using CMS.Activities;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.ContactManagement;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: UIPage(
    uiPageType: typeof(ContactActivityPathPage),
    parentType: typeof(ContactEditSection),
    slug: "activity-path",
    name: "Activity path",
    templateName: "@kentico-community/portal-web-admin/ContactActivityPathLayout",
    order: 1002,
    Icon = Icons.Graph)]

namespace Kentico.Community.Portal.Admin.Features.ContactManagement;

[UIEvaluatePermission(SystemPermissions.VIEW)]
public class ContactActivityPathPage(
    TimeProvider clock,
    IInfoProvider<ActivityTypeInfo> activityTypeProvider,
    IInfoProvider<ContactInfo> contactProvider) : Page<ContactActivityPathLayoutClientProperties>
{
    private const int DefaultWindowDays = 30;
    private static readonly TimeSpan defaultActivityDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan maxActivityDuration = TimeSpan.FromHours(6);

    private readonly TimeProvider clock = clock;
    private readonly IInfoProvider<ActivityTypeInfo> activityTypeProvider = activityTypeProvider;
    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;

    [PageParameter(typeof(IntPageModelBinder), typeof(ContactEditSection))]
    public int ObjectId { get; set; }

    public override async Task<ContactActivityPathLayoutClientProperties> ConfigureTemplateProperties(ContactActivityPathLayoutClientProperties properties)
    {
        var defaultWindow = GetDefaultWindow();
        var contact = await GetContact(cancellationToken: CancellationToken.None);

        properties.Title = "Contact activity path";
        properties.Description = contact is null
            ? "Review the selected contact's recorded activities as a time-ordered path within the selected range."
            : $"Review {GetContactDisplayName(contact)} across the selected range to see the recorded order of tracked activities.";
        properties.Stats = await GetDashboard(new(defaultWindow.StartDate, defaultWindow.EndDateExclusive.AddDays(-1)), CancellationToken.None);

        return properties;
    }

    [PageCommand(CommandName = "LOADDATA")]
    public async Task<ICommandResponse> PageCommandHandler(ContactActivityPathQuery query, CancellationToken cancellationToken)
    {
        var stats = await GetDashboard(query, cancellationToken);
        return ResponseFrom(stats);
    }

    private async Task<ContactActivityPathDashboard> GetDashboard(ContactActivityPathQuery query, CancellationToken cancellationToken)
    {
        var window = GetWindow(query.RangeStart, query.RangeEnd);
        var contact = await GetContact(cancellationToken);

        if (contact is null)
        {
            return new(
                DateOnly.FromDateTime(window.StartDate),
                DateOnly.FromDateTime(window.EndDateExclusive.AddDays(-1)),
                new("Unknown contact", string.Empty),
                new(0, 0, 0, "No activity in range", "No activity in range"),
                []);
        }

        var activityTypeMetadata = await GetActivityTypeMetadata(cancellationToken);
        var rows = await GetActivityRows(window, cancellationToken);
        var items = BuildPathItems(rows, activityTypeMetadata);
        var overview = BuildOverview(items);

        return new(
            DateOnly.FromDateTime(window.StartDate),
            DateOnly.FromDateTime(window.EndDateExclusive.AddDays(-1)),
            new(GetContactDisplayName(contact), contact.ContactEmail),
            overview,
            items);
    }

    private async Task<ContactInfo?> GetContact(CancellationToken cancellationToken) =>
        (await contactProvider
            .Get()
            .WhereEquals(nameof(ContactInfo.ContactID), ObjectId)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
        .FirstOrDefault();

    private static ContactActivityPathOverview BuildOverview(IReadOnlyList<ContactActivityPathItem> items)
    {
        if (items.Count == 0)
        {
            return new(0, 0, 0, "No activity in range", "No activity in range");
        }

        return new(
            items.Count,
            items.Select(item => item.ActivityTypeKey).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            items.Select(item => item.DayLabel).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            items[0].OccurredAtLabel,
            items[^1].OccurredAtLabel);
    }

    private static ImmutableList<ContactActivityPathItem> BuildPathItems(
        IReadOnlyList<ContactActivityRow> rows,
        IReadOnlyDictionary<string, string> activityTypeMetadata)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var items = new List<ContactActivityPathItem>(rows.Count);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var nextStart = i < rows.Count - 1
                ? rows[i + 1].ActivityCreated
                : row.ActivityCreated.Add(defaultActivityDuration);
            var endTime = GetEndTime(row.ActivityCreated, nextStart);
            string activityLabel = activityTypeMetadata.GetValueOrDefault(row.ActivityTypeKey) ?? ToTitleCase(row.ActivityTypeKey);
            string occurredAtLabel = row.ActivityCreated.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);

            items.Add(new(
                $"activity-{row.ActivityID}",
                row.ActivityTypeKey,
                activityLabel,
                row.ActivityCreated.ToString("dd MMM", CultureInfo.InvariantCulture),
                row.ActivityCreated,
                endTime,
                occurredAtLabel,
                BuildActivityDetail(row, occurredAtLabel)));
        }

        return [.. items];
    }

    private static string BuildActivityDetail(ContactActivityRow row, string occurredAtLabel) =>
        string.IsNullOrWhiteSpace(row.ActivityValue)
            ? $"Recorded {occurredAtLabel}"
            : row.ActivityValue;

    private static DateTime GetEndTime(DateTime startTime, DateTime nextStart)
    {
        var candidateEnd = nextStart > startTime
            ? nextStart
            : startTime.Add(defaultActivityDuration);
        var maxEnd = startTime.Add(maxActivityDuration);

        return candidateEnd > maxEnd ? maxEnd : candidateEnd;
    }

    private async Task<IReadOnlyDictionary<string, string>> GetActivityTypeMetadata(CancellationToken cancellationToken)
    {
        var activityTypes = await activityTypeProvider
            .Get()
            .WhereTrue(nameof(ActivityTypeInfo.ActivityTypeEnabled))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return activityTypes.ToDictionary(
            item => item.ActivityTypeName,
            item => string.IsNullOrWhiteSpace(item.ActivityTypeDisplayName) ? item.ActivityTypeName : item.ActivityTypeDisplayName,
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<ImmutableList<ContactActivityRow>> GetActivityRows(ContactActivityPathWindow window, CancellationToken cancellationToken)
    {
        const string query = """
        SELECT
            A.[ActivityID],
            A.[ActivityCreated],
            COALESCE(NULLIF(A.[ActivityType], ''), 'unknown-activity') AS [ActivityTypeKey],
            COALESCE(A.[ActivityValue], '') AS [ActivityValue]
        FROM [OM_Activity] A
        WHERE A.[ActivityContactID] = @ContactID
            AND A.[ActivityCreated] >= @StartDate
            AND A.[ActivityCreated] < @EndDateExclusive
        ORDER BY A.[ActivityCreated] ASC, A.[ActivityID] ASC
        """;

        var parameters = new QueryDataParameters
        {
            new DataParameter("@ContactID", ObjectId),
            new DataParameter("@StartDate", window.StartDate),
            new DataParameter("@EndDateExclusive", window.EndDateExclusive),
        };

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);
        var rows = new List<ContactActivityRow>();

        while (reader.Read())
        {
            rows.Add(new(
                reader.GetInt32("ActivityID"),
                reader.GetDateTime("ActivityCreated"),
                reader.GetString("ActivityTypeKey"),
                reader.GetString("ActivityValue")));
        }

        return [.. rows];
    }

    private ContactActivityPathWindow GetDefaultWindow()
    {
        var today = clock.GetUtcNow().UtcDateTime.Date;
        return GetWindow(today.AddDays(-(DefaultWindowDays - 1)), today);
    }

    private static ContactActivityPathWindow GetWindow(DateTime rangeStart, DateTime rangeEnd)
    {
        var normalizedStart = rangeStart.Date;
        var normalizedEnd = rangeEnd.Date;

        if (normalizedEnd < normalizedStart)
        {
            (normalizedStart, normalizedEnd) = (normalizedEnd, normalizedStart);
        }

        return new(normalizedStart, normalizedEnd.AddDays(1));
    }

    private static string GetContactDisplayName(ContactInfo contact)
    {
        string fullName = string.Join(" ", new[] { contact.ContactFirstName, contact.ContactLastName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        if (!string.IsNullOrWhiteSpace(contact.ContactEmail))
        {
            return contact.ContactEmail;
        }

        return $"Contact {contact.ContactID}";
    }

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown activity";
        }

        string normalized = value.Replace('-', ' ').Replace('_', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
    }

    private sealed record ContactActivityPathWindow(DateTime StartDate, DateTime EndDateExclusive);
    private sealed record ContactActivityRow(int ActivityID, DateTime ActivityCreated, string ActivityTypeKey, string ActivityValue);
}
