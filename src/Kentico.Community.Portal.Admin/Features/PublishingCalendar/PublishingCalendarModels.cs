using Kentico.Xperience.Admin.Base;

namespace Kentico.Community.Portal.Admin.Features.PublishingCalendar;

public class PublishingCalendarPageClientProperties : TemplateClientProperties
{
    public string LoadCalendarCommandName { get; set; } = "";
    public string LoadDayItemsCommandName { get; set; } = "";
    public int DayItemsPageSize { get; set; }
    public IReadOnlyList<PublishingCalendarContentTypeOption> ContentTypes { get; set; } = [];
    public IReadOnlyList<PublishingCalendarChannelOption> Channels { get; set; } = [];
    public IReadOnlyList<PublishingCalendarWorkflowOption> Workflows { get; set; } = [];
}

/// <summary>
/// A calendar day in the browser's time zone. <paramref name="Key"/> is the client's yyyy-mm-dd day key,
/// <paramref name="Start"/>/<paramref name="End"/> are the day's instant boundaries (end exclusive).
/// </summary>
public record PublishingCalendarDay(string Key, DateTimeOffset Start, DateTimeOffset End);

/// <summary>
/// Content type / channel / workflow step filters. OR within a filter, AND across filters. Empty = no filtering.
/// Channel ID <see cref="PublishingCalendarChannelOption.CONTENT_HUB_CHANNEL_ID"/> matches reusable (channel-less) items.
/// Workflow step IDs match variants currently in that step.
/// </summary>
public record PublishingCalendarFilters(int[]? ContentTypeIds, int[]? ChannelIds, int[]? WorkflowStepIds);

public record LoadCalendarParams(PublishingCalendarDay[] Days, PublishingCalendarFilters? Filters);

public record LoadDayItemsParams(PublishingCalendarDay Day, string Status, int Skip, int Take, PublishingCalendarFilters? Filters);

/// <summary>
/// Accurate per day/status counts plus item rows capped per day/status.
/// </summary>
public record PublishingCalendarResult(IReadOnlyList<PublishingCalendarDayStatusCount> Counts, IReadOnlyList<PublishingCalendarEvent> Items);

public record PublishingCalendarDayStatusCount(string Day, string Status, int Count);

public record PublishingCalendarDayItemsResult(int Total, IReadOnlyList<PublishingCalendarEvent> Items);

public record PublishingCalendarContentTypeOption(int Id, string Name, string Icon);

public record PublishingCalendarChannelOption(int Id, string Name, string ChannelType)
{
    /// <summary>Pseudo channel for reusable content hub items (no channel).</summary>
    public const int CONTENT_HUB_CHANNEL_ID = 0;
}

/// <summary>A workflow and its steps in step order.</summary>
public record PublishingCalendarWorkflowOption(int Id, string Name, IReadOnlyList<PublishingCalendarWorkflowStepOption> Steps);

/// <param name="Count">Language variants currently in the step.</param>
public record PublishingCalendarWorkflowStepOption(int Id, string Name, string Icon, int Count);

/// <summary>
/// Calendar entry kinds, shared with the client (publishing-calendar status keys).
/// </summary>
public static class PublishingCalendarEventStatus
{
    public const string PUBLISHED = "published";
    public const string SCHEDULED_PUBLISH = "scheduled-publish";
    public const string SCHEDULED_UNPUBLISH = "scheduled-unpublish";
    public const string IN_WORKFLOW = "in-workflow";
    public const string DRAFT = "draft";
    public const string UNPUBLISHED = "unpublished";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        PUBLISHED, SCHEDULED_PUBLISH, SCHEDULED_UNPUBLISH, IN_WORKFLOW, DRAFT, UNPUBLISHED
    };
}

/// <summary>
/// A single publishing calendar entry for a content item language variant.
/// </summary>
/// <param name="Id">Unique per language variant and status kind.</param>
/// <param name="Day">Client day key the entry falls on.</param>
/// <param name="Date">Instant of the change or schedule (with offset).</param>
/// <param name="Status">One of <see cref="PublishingCalendarEventStatus"/>.</param>
/// <param name="VersionStatus">Latest <see cref="CMS.ContentEngine.VersionStatus"/> name.</param>
public record PublishingCalendarEvent(
    string Id,
    string Day,
    string Title,
    DateTimeOffset Date,
    string Status,
    string VersionStatus,
    string ContentType,
    string ContentTypeType,
    string ContentTypeIcon,
    string ChannelName,
    string ChannelType,
    string WorkspaceName,
    string LanguageName,
    string LanguageDisplayName,
    string WorkflowName,
    string WorkflowStepName,
    string WorkflowStepIcon,
    DateTimeOffset? ScheduledPublishWhen,
    DateTimeOffset? ScheduledUnpublishWhen,
    DateTimeOffset ModifiedWhen,
    string ModifiedBy,
    string EditUrl);
