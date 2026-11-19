using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using CMS.ContentEngine;
using CMS.ContentWorkflowEngine;
using CMS.DataEngine;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;
using Kentico.Xperience.Admin.Headless.UIPages;
using Kentico.Xperience.Admin.Websites.UIPages;

namespace Kentico.Community.Portal.Admin.Features.PublishingCalendar;

/// <summary>
/// SQL queries behind the publishing calendar. Xperience stores date/time values in the server's
/// time zone, so client day boundaries (instants) are converted to server local time for querying and
/// results are returned with the server offset.
/// </summary>
public class PublishingCalendarDataSource(IPageLinkGenerator pageLinkGenerator)
{
    /// <summary>Published items returned per day before the client has to page ("load more").</summary>
    public const int PUBLISHED_ITEMS_PER_DAY = 10;
    /// <summary>Safety cap for all other statuses per day.</summary>
    public const int ITEMS_PER_DAY_CAP = 200;
    public const int DAY_ITEMS_PAGE_SIZE = 50;
    private const int MAX_DAYS = 62;

    private readonly IPageLinkGenerator pageLinkGenerator = pageLinkGenerator;

    /// <summary>
    /// Every calendar entry for a metadata row: the activity (or scheduled publish) entry and the scheduled
    /// unpublish entry, bucketed into the requested client days and filtered by content type / channel / workflow step.
    /// </summary>
    private const string EVENTS_CTE = """
        Days AS (
            SELECT DayKey, DayStart, DayEnd FROM (VALUES {0}) d(DayKey, DayStart, DayEnd)
        ),
        Events AS (
            SELECT
                lm.ContentItemLanguageMetadataID AS MetadataID,
                e.EventDate,
                e.Status,
                d.DayKey
            FROM CMS_ContentItemLanguageMetadata lm
            INNER JOIN CMS_ContentItem ci
                ON lm.ContentItemLanguageMetadataContentItemID = ci.ContentItemID
            LEFT JOIN CMS_ContentWorkflowStep wfs
                ON wfs.ContentWorkflowStepID = lm.ContentItemLanguageMetadataContentWorkflowStepID
            CROSS APPLY (VALUES
                (
                    COALESCE(lm.ContentItemLanguageMetadataScheduledPublishWhen, lm.ContentItemLanguageMetadataModifiedWhen),
                    CASE
                        WHEN lm.ContentItemLanguageMetadataScheduledPublishWhen IS NOT NULL THEN 'scheduled-publish'
                        WHEN lm.ContentItemLanguageMetadataLatestVersionStatus = @PublishedVersion THEN 'published'
                        WHEN lm.ContentItemLanguageMetadataLatestVersionStatus = @UnpublishedVersion THEN 'unpublished'
                        WHEN wfs.ContentWorkflowStepType = @CustomWorkflowStep THEN 'in-workflow'
                        ELSE 'draft'
                    END
                ),
                (lm.ContentItemLanguageMetadataScheduledUnpublishWhen, 'scheduled-unpublish')
            ) e(EventDate, Status)
            INNER JOIN Days d
                ON e.EventDate >= d.DayStart AND e.EventDate < d.DayEnd
            WHERE e.EventDate >= @RangeStart AND e.EventDate < @RangeEnd
            {1}
        )
        """;

    private const string DETAIL_COLUMNS = """
            lm.ContentItemLanguageMetadataID,
            lm.ContentItemLanguageMetadataDisplayName,
            c.ClassDisplayName,
            c.ClassContentTypeType,
            c.ClassIconClass,
            lm.ContentItemLanguageMetadataModifiedWhen,
            lm.ContentItemLanguageMetadataLatestVersionStatus,
            ws.ContentWorkflowStepDisplayName,
            ws.ContentWorkflowStepIconClass,
            w.ContentWorkflowDisplayName,
            lm.ContentItemLanguageMetadataScheduledPublishWhen,
            lm.ContentItemLanguageMetadataScheduledUnpublishWhen,
            u.UserName,
            u.FirstName,
            u.LastName,
            ci.ContentItemID,
            ci.ContentItemWorkspaceID,
            wsp.WorkspaceDisplayName,
            ch.ChannelDisplayName,
            ch.ChannelType,
            cl.ContentLanguageName,
            cl.ContentLanguageDisplayName,
            wc.WebsiteChannelID,
            wp.WebPageItemID,
            hc.HeadlessChannelID,
            hi.HeadlessItemID,
            ec.EmailChannelID,
            econf.EmailConfigurationID
        """;

    private const string DETAIL_JOINS = """
        INNER JOIN CMS_ContentItemLanguageMetadata lm
            ON lm.ContentItemLanguageMetadataID = r.MetadataID
        INNER JOIN CMS_ContentItem ci
            ON lm.ContentItemLanguageMetadataContentItemID = ci.ContentItemID
        INNER JOIN CMS_Class c
            ON ci.ContentItemContentTypeID = c.ClassID
        INNER JOIN CMS_ContentLanguage cl
            ON cl.ContentLanguageID = lm.ContentItemLanguageMetadataContentLanguageID
        LEFT JOIN CMS_User u
            ON u.UserID = lm.ContentItemLanguageMetadataModifiedByUserID
        LEFT JOIN CMS_Workspace wsp
            ON wsp.WorkspaceID = ci.ContentItemWorkspaceID
        LEFT JOIN CMS_Channel ch
            ON ch.ChannelID = ci.ContentItemChannelID
        LEFT JOIN CMS_ContentWorkflowStep ws
            ON ws.ContentWorkflowStepID = lm.ContentItemLanguageMetadataContentWorkflowStepID
        LEFT JOIN CMS_ContentWorkflow w
            ON ws.ContentWorkflowStepWorkflowID = w.ContentWorkflowID
        LEFT JOIN CMS_WebsiteChannel wc
            ON wc.WebsiteChannelChannelID = ci.ContentItemChannelID
        LEFT JOIN CMS_WebPageItem wp
            ON wp.WebPageItemContentItemID = ci.ContentItemID
        LEFT JOIN CMS_HeadlessChannel hc
            ON hc.HeadlessChannelChannelID = ci.ContentItemChannelID
        LEFT JOIN CMS_HeadlessItem hi
            ON hi.HeadlessItemContentItemID = ci.ContentItemID
        LEFT JOIN EmailLibrary_EmailChannel ec
            ON ec.EmailChannelChannelID = ci.ContentItemChannelID
        LEFT JOIN EmailLibrary_EmailConfiguration econf
            ON econf.EmailConfigurationContentItemID = ci.ContentItemID
        """;

    /// <summary>
    /// Per day/status counts for all requested days plus the first items of each day/status group
    /// (<see cref="PUBLISHED_ITEMS_PER_DAY"/> for published, <see cref="ITEMS_PER_DAY_CAP"/> otherwise).
    /// </summary>
    public async Task<PublishingCalendarResult> LoadCalendar(LoadCalendarParams commandParams, CancellationToken cancellationToken)
    {
        var days = (commandParams.Days ?? []).Take(MAX_DAYS).ToArray();
        if (days.Length == 0)
        {
            return new([], []);
        }

        var parameters = CreateBaseParameters(days);
        parameters.Add("@PublishedCap", PUBLISHED_ITEMS_PER_DAY);
        parameters.Add("@Cap", ITEMS_PER_DAY_CAP);

        string query = $"""
            WITH {BuildEventsCte(days, commandParams.Filters)},
            Ranked AS (
                SELECT
                    MetadataID, EventDate, Status, DayKey,
                    ROW_NUMBER() OVER (PARTITION BY DayKey, Status ORDER BY EventDate, MetadataID) AS RowNumber,
                    COUNT(*) OVER (PARTITION BY DayKey, Status) AS GroupTotal
                FROM Events
            )
            SELECT
                r.DayKey, r.Status, r.EventDate, r.GroupTotal,
                {DETAIL_COLUMNS}
            FROM Ranked r
            {DETAIL_JOINS}
            WHERE r.RowNumber <= CASE WHEN r.Status = 'published' THEN @PublishedCap ELSE @Cap END
            ORDER BY r.EventDate, r.MetadataID
            """;

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);

        var items = new List<PublishingCalendarEvent>();
        var counts = new Dictionary<(string Day, string Status), int>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var item = ReadEvent(reader);
            items.Add(item);
            counts[(item.Day, item.Status)] = reader.GetInt32("GroupTotal");
        }

        return new(
            [.. counts.Select(c => new PublishingCalendarDayStatusCount(c.Key.Day, c.Key.Status, c.Value))],
            [.. items.DistinctBy(e => e.Id)]);
    }

    /// <summary>
    /// A page of items for one day and status, ordered like <see cref="LoadCalendar"/>.
    /// </summary>
    public async Task<PublishingCalendarDayItemsResult> LoadDayItems(LoadDayItemsParams commandParams, CancellationToken cancellationToken)
    {
        if (!PublishingCalendarEventStatus.All.Contains(commandParams.Status))
        {
            return new(0, []);
        }

        PublishingCalendarDay[] days = [commandParams.Day];
        var parameters = CreateBaseParameters(days);
        parameters.Add("@Status", commandParams.Status);
        parameters.Add("@Skip", Math.Max(0, commandParams.Skip));
        parameters.Add("@Take", Math.Clamp(commandParams.Take, 1, ITEMS_PER_DAY_CAP));

        string query = $"""
            WITH {BuildEventsCte(days, commandParams.Filters)},
            Ranked AS (
                SELECT MetadataID, EventDate, Status, DayKey, COUNT(*) OVER () AS GroupTotal
                FROM Events
                WHERE Status = @Status
                ORDER BY EventDate, MetadataID
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            SELECT
                r.DayKey, r.Status, r.EventDate, r.GroupTotal,
                {DETAIL_COLUMNS}
            FROM Ranked r
            {DETAIL_JOINS}
            ORDER BY r.EventDate, r.MetadataID
            """;

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);

        var items = new List<PublishingCalendarEvent>();
        int total = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadEvent(reader));
            total = reader.GetInt32("GroupTotal");
        }

        return new(total, [.. items.DistinctBy(e => e.Id)]);
    }

    /// <summary>Content types that have at least one content item.</summary>
    public async Task<IReadOnlyList<PublishingCalendarContentTypeOption>> LoadContentTypeOptions(CancellationToken cancellationToken)
    {
        const string query = """
            SELECT c.ClassID, c.ClassDisplayName, c.ClassIconClass
            FROM CMS_Class c
            WHERE EXISTS (SELECT 1 FROM CMS_ContentItem ci WHERE ci.ContentItemContentTypeID = c.ClassID)
            ORDER BY c.ClassDisplayName
            """;

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, [], QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);

        var options = new List<PublishingCalendarContentTypeOption>();
        while (await reader.ReadAsync(cancellationToken))
        {
            options.Add(new(
                reader.GetInt32("ClassID"),
                reader.GetString("ClassDisplayName"),
                GetStringOrEmpty(reader, "ClassIconClass")));
        }

        return options;
    }

    /// <summary>All channels plus the content hub (reusable items) pseudo channel.</summary>
    public async Task<IReadOnlyList<PublishingCalendarChannelOption>> LoadChannelOptions(CancellationToken cancellationToken)
    {
        const string query = """
            SELECT ChannelID, ChannelDisplayName, ChannelType
            FROM CMS_Channel
            ORDER BY ChannelDisplayName
            """;

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, [], QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);

        var options = new List<PublishingCalendarChannelOption>
        {
            new(PublishingCalendarChannelOption.CONTENT_HUB_CHANNEL_ID, "Content hub (reusable)", "")
        };
        while (await reader.ReadAsync(cancellationToken))
        {
            options.Add(new(
                reader.GetInt32("ChannelID"),
                reader.GetString("ChannelDisplayName"),
                GetStringOrEmpty(reader, "ChannelType")));
        }

        return options;
    }

    /// <summary>
    /// Workflows with their steps (step order) and how many variants are currently in each step.
    /// Final steps are only listed when something is in them; workflows without listed steps are omitted.
    /// </summary>
    public async Task<IReadOnlyList<PublishingCalendarWorkflowOption>> LoadWorkflowOptions(CancellationToken cancellationToken)
    {
        const string query = """
            SELECT
                w.ContentWorkflowID,
                w.ContentWorkflowDisplayName,
                s.ContentWorkflowStepID,
                s.ContentWorkflowStepDisplayName,
                s.ContentWorkflowStepIconClass,
                COALESCE(sc.ItemCount, 0) AS ItemCount
            FROM CMS_ContentWorkflow w
            INNER JOIN CMS_ContentWorkflowStep s
                ON s.ContentWorkflowStepWorkflowID = w.ContentWorkflowID
            LEFT JOIN (
                SELECT ContentItemLanguageMetadataContentWorkflowStepID AS StepID, COUNT(*) AS ItemCount
                FROM CMS_ContentItemLanguageMetadata
                WHERE ContentItemLanguageMetadataContentWorkflowStepID IS NOT NULL
                GROUP BY ContentItemLanguageMetadataContentWorkflowStepID
            ) sc
                ON sc.StepID = s.ContentWorkflowStepID
            WHERE s.ContentWorkflowStepType <> @FinalWorkflowStep OR sc.ItemCount > 0
            ORDER BY w.ContentWorkflowDisplayName, w.ContentWorkflowID, s.ContentWorkflowStepOrder
            """;

        var parameters = new QueryDataParameters
        {
            { "@FinalWorkflowStep", (int)ContentWorkflowStepType.SystemFinal },
        };

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);

        var workflows = new List<(int Id, string Name, List<PublishingCalendarWorkflowStepOption> Steps)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            int workflowId = reader.GetInt32("ContentWorkflowID");
            if (workflows.Count == 0 || workflows[^1].Id != workflowId)
            {
                workflows.Add((workflowId, reader.GetString("ContentWorkflowDisplayName"), []));
            }

            workflows[^1].Steps.Add(new(
                reader.GetInt32("ContentWorkflowStepID"),
                GetStringOrEmpty(reader, "ContentWorkflowStepDisplayName"),
                GetStringOrEmpty(reader, "ContentWorkflowStepIconClass"),
                reader.GetInt32("ItemCount")));
        }

        return [.. workflows.Select(w => new PublishingCalendarWorkflowOption(w.Id, w.Name, w.Steps))];
    }

    private static QueryDataParameters CreateBaseParameters(PublishingCalendarDay[] days)
    {
        var parameters = new QueryDataParameters
        {
            { "@PublishedVersion", (int)VersionStatus.Published },
            { "@UnpublishedVersion", (int)VersionStatus.Unpublished },
            { "@CustomWorkflowStep", (int)ContentWorkflowStepType.Custom },
            { "@RangeStart", days.Min(d => d.Start).LocalDateTime },
            { "@RangeEnd", days.Max(d => d.End).LocalDateTime },
        };

        for (int i = 0; i < days.Length; i++)
        {
            parameters.Add($"@DayKey{i}", days[i].Key);
            parameters.Add($"@DayStart{i}", days[i].Start.LocalDateTime);
            parameters.Add($"@DayEnd{i}", days[i].End.LocalDateTime);
        }

        return parameters;
    }

    private static string BuildEventsCte(PublishingCalendarDay[] days, PublishingCalendarFilters? filters)
    {
        string values = string.Join(", ", days.Select((_, i) => $"(@DayKey{i}, @DayStart{i}, @DayEnd{i})"));

        return string.Format(CultureInfo.InvariantCulture, EVENTS_CTE, values, BuildFilterClause(filters));
    }

    /// <summary>OR within a filter, AND across filters. IDs are integers, so inlining them is injection safe.</summary>
    private static string BuildFilterClause(PublishingCalendarFilters? filters)
    {
        var clause = new StringBuilder();

        int[] contentTypeIds = filters?.ContentTypeIds ?? [];
        if (contentTypeIds.Length > 0)
        {
            clause.Append(CultureInfo.InvariantCulture, $" AND ci.ContentItemContentTypeID IN ({JoinIds(contentTypeIds)})");
        }

        int[] channelIds = filters?.ChannelIds ?? [];
        if (channelIds.Length > 0)
        {
            var conditions = new List<string>();
            int[] realChannelIds = [.. channelIds.Where(id => id != PublishingCalendarChannelOption.CONTENT_HUB_CHANNEL_ID)];
            if (realChannelIds.Length > 0)
            {
                conditions.Add($"ci.ContentItemChannelID IN ({JoinIds(realChannelIds)})");
            }
            if (channelIds.Contains(PublishingCalendarChannelOption.CONTENT_HUB_CHANNEL_ID))
            {
                conditions.Add("ci.ContentItemChannelID IS NULL");
            }
            clause.Append(CultureInfo.InvariantCulture, $" AND ({string.Join(" OR ", conditions)})");
        }

        int[] workflowStepIds = filters?.WorkflowStepIds ?? [];
        if (workflowStepIds.Length > 0)
        {
            clause.Append(CultureInfo.InvariantCulture, $" AND lm.ContentItemLanguageMetadataContentWorkflowStepID IN ({JoinIds(workflowStepIds)})");
        }

        return clause.ToString();
    }

    private static string JoinIds(IEnumerable<int> ids) =>
        string.Join(",", ids.Distinct().Select(id => id.ToString(CultureInfo.InvariantCulture)));

    private PublishingCalendarEvent ReadEvent(DbDataReader reader)
    {
        int metadataId = reader.GetInt32("ContentItemLanguageMetadataID");
        string status = reader.GetString("Status");
        string contentTypeType = GetStringOrEmpty(reader, "ClassContentTypeType");
        string languageName = reader.GetString("ContentLanguageName");
        var versionStatus = (VersionStatus)reader.GetInt32("ContentItemLanguageMetadataLatestVersionStatus");

        string editUrl = BuildEditUrl(
            contentTypeType,
            reader.GetInt32("ContentItemID"), GetInt32OrZero(reader, "ContentItemWorkspaceID"), languageName,
            GetInt32OrZero(reader, "WebsiteChannelID"), GetInt32OrZero(reader, "WebPageItemID"),
            GetInt32OrZero(reader, "HeadlessChannelID"), GetInt32OrZero(reader, "HeadlessItemID"),
            GetInt32OrZero(reader, "EmailChannelID"), GetInt32OrZero(reader, "EmailConfigurationID"));

        return new(
            Id: $"{metadataId}-{status}",
            Day: reader.GetString("DayKey"),
            Title: reader.GetString("ContentItemLanguageMetadataDisplayName"),
            Date: ToServerOffset(reader.GetDateTime("EventDate")),
            Status: status,
            VersionStatus: versionStatus.ToString(),
            ContentType: reader.GetString("ClassDisplayName"),
            ContentTypeType: contentTypeType,
            ContentTypeIcon: GetStringOrEmpty(reader, "ClassIconClass"),
            ChannelName: GetStringOrEmpty(reader, "ChannelDisplayName"),
            ChannelType: GetStringOrEmpty(reader, "ChannelType"),
            WorkspaceName: GetStringOrEmpty(reader, "WorkspaceDisplayName"),
            LanguageName: languageName,
            LanguageDisplayName: GetStringOrEmpty(reader, "ContentLanguageDisplayName"),
            WorkflowName: GetStringOrEmpty(reader, "ContentWorkflowDisplayName"),
            WorkflowStepName: GetStringOrEmpty(reader, "ContentWorkflowStepDisplayName"),
            WorkflowStepIcon: GetStringOrEmpty(reader, "ContentWorkflowStepIconClass"),
            ScheduledPublishWhen: GetDateTimeOrNull(reader, "ContentItemLanguageMetadataScheduledPublishWhen"),
            ScheduledUnpublishWhen: GetDateTimeOrNull(reader, "ContentItemLanguageMetadataScheduledUnpublishWhen"),
            ModifiedWhen: ToServerOffset(reader.GetDateTime("ContentItemLanguageMetadataModifiedWhen")),
            ModifiedBy: FormatUserName(
                GetStringOrEmpty(reader, "FirstName"),
                GetStringOrEmpty(reader, "LastName"),
                GetStringOrEmpty(reader, "UserName")),
            EditUrl: editUrl);
    }

    /// <summary>Database values are server local time; attach the server offset so the browser can convert.</summary>
    private static DateTimeOffset ToServerOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Local));

    private static string FormatUserName(string firstName, string lastName, string userName)
    {
        string fullName = $"{firstName} {lastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName) ? userName : fullName;
    }

    private static string GetStringOrEmpty(DbDataReader reader, string column) =>
        reader.IsDBNull(column) ? "" : reader.GetString(column);

    private static int GetInt32OrZero(DbDataReader reader, string column) =>
        reader.IsDBNull(column) ? 0 : reader.GetInt32(column);

    private static DateTimeOffset? GetDateTimeOrNull(DbDataReader reader, string column) =>
        reader.IsDBNull(column) ? null : ToServerOffset(reader.GetDateTime(column));

    private string BuildEditUrl(
        string contentTypeType,
        int contentItemId, int workspaceId, string languageName,
        int websiteChannelId, int webPageItemId,
        int headlessChannelId, int headlessItemId,
        int emailChannelId, int emailConfigId)
    {
        string path = contentTypeType switch
        {
            "Website" when websiteChannelId > 0 && webPageItemId > 0 =>
                pageLinkGenerator.GetPath<ContentTab>(new PageParameterValues
                {
                    { typeof(WebPageLayout), $"{languageName}_{webPageItemId}" },
                    { typeof(WebPagesApplication), $"webpages-{websiteChannelId}" },
                }),
            "Email" when emailChannelId > 0 && emailConfigId > 0 =>
                pageLinkGenerator.GetPath<EmailContentTab>(new PageParameterValues
                {
                    { typeof(EmailEditLayout), emailConfigId },
                    { typeof(EmailChannelContentLanguage), languageName },
                    { typeof(EmailChannelApplication), $"emails-{emailChannelId}" },
                }),
            "Headless" when headlessChannelId > 0 && headlessItemId > 0 =>
                pageLinkGenerator.GetPath<HeadlessContentTab>(new PageParameterValues
                {
                    { typeof(HeadlessEditLayout), headlessItemId },
                    { typeof(HeadlessChannelContentLanguage), languageName },
                    { typeof(HeadlessChannelApplication), $"headless-{headlessChannelId}" },
                }),
            _ when contentItemId > 0 =>
                pageLinkGenerator.GetPath<ContentItemEdit>(new PageParameterValues
                {
                    { typeof(ContentItemEditSection), contentItemId },
                    { typeof(ContentHubFolder), ContentHubSlugs.ALL_CONTENT_ITEMS },
                    { typeof(ContentHubContentLanguage), languageName },
                    { typeof(ContentHubWorkspace), workspaceId },
                }),
            _ => ""
        };

        return path.StartsWith('/') ? path[1..] : path;
    }
}
