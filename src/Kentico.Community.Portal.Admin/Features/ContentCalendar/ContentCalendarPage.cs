using System.Data;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.ContentCalendar;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;
using Kentico.Xperience.Admin.Headless.UIPages;
using Kentico.Xperience.Admin.Websites.UIPages;

[assembly: UIPage(
    uiPageType: typeof(ContentCalendarPage),
    parentType: typeof(ContentCalendarApplicationPage),
    slug: "calendar",
    name: "Calendar",
    templateName: "@kentico-community/portal-web-admin/ContentCalendar",
    order: 0,
    Icon = Icons.Calendar)]

namespace Kentico.Community.Portal.Admin.Features.ContentCalendar;

[UIPermission(SystemPermissions.VIEW)]
public class ContentCalendarPage(IPageLinkGenerator pageLinkGenerator) : Page<ContentCalendarPageClientProperties>
{
    private readonly IPageLinkGenerator pageLinkGenerator = pageLinkGenerator;

    public const string LOAD_EVENTS_COMMAND = "LoadEvents";

    public override async Task<ContentCalendarPageClientProperties> ConfigureTemplateProperties(ContentCalendarPageClientProperties properties)
    {
        properties.LoadEventsCommandName = LOAD_EVENTS_COMMAND;
        var today = DateTime.UtcNow;
        var start = new DateTime(today.Year, today.Month, 1);
        var end = start.AddMonths(1);
        properties.InitialEvents = await LoadCalendarEvents(start, end, CancellationToken.None);

        return properties;
    }

    [PageCommand(CommandName = LOAD_EVENTS_COMMAND)]
    public async Task<ICommandResponse> LoadEvents(LoadEventsParams commandParams, CancellationToken cancellationToken)
    {
        var events = await LoadCalendarEvents(commandParams.Start, commandParams.End, cancellationToken);

        return ResponseFrom(events);
    }

    /// <summary>Published version status value from ContentItemLanguageMetadataLatestVersionStatus.</summary>
    private const int VersionStatusPublished = 2;

    private async Task<ContentCalendarEvent[]> LoadCalendarEvents(DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        string query = """
            SELECT
                lm.ContentItemLanguageMetadataDisplayName,
                c.ClassDisplayName,
                c.ClassContentTypeType,
                lm.ContentItemLanguageMetadataModifiedWhen,
                lm.ContentItemLanguageMetadataLatestVersionStatus,
                ws.ContentWorkflowStepDisplayName,
                ws.ContentWorkflowStepIconClass,
                w.ContentWorkflowDisplayName,
                lm.ContentItemLanguageMetadataScheduledPublishWhen,
                lm.ContentItemLanguageMetadataScheduledUnpublishWhen,
                ci.ContentItemID,
                ci.ContentItemWorkspaceID,
                cl.ContentLanguageName,
                wc.WebsiteChannelID,
                wp.WebPageItemID,
                hc.HeadlessChannelID,
                hi.HeadlessItemID,
                ec.EmailChannelID,
                econf.EmailConfigurationID
            FROM CMS_ContentItemLanguageMetadata lm
            INNER JOIN CMS_ContentItem ci
                ON lm.ContentItemLanguageMetadataContentItemID = ci.ContentItemID
            INNER JOIN CMS_Class c
                ON ci.ContentItemContentTypeID = c.ClassID
            INNER JOIN CMS_ContentLanguage cl
                ON cl.ContentLanguageID = lm.ContentItemLanguageMetadataContentLanguageID
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
            WHERE (
                lm.ContentItemLanguageMetadataModifiedWhen >= @Start
                AND lm.ContentItemLanguageMetadataModifiedWhen < @End
                AND lm.ContentItemLanguageMetadataLatestVersionStatus != @Published
            )
            OR (
                lm.ContentItemLanguageMetadataScheduledPublishWhen >= @Start
                AND lm.ContentItemLanguageMetadataScheduledPublishWhen < @End
            )
            OR (
                lm.ContentItemLanguageMetadataScheduledUnpublishWhen >= @Start
                AND lm.ContentItemLanguageMetadataScheduledUnpublishWhen < @End
            )
            """;

        var parameters = new QueryDataParameters
        {
            new DataParameter("@Start", start),
            new DataParameter("@End", end),
            new DataParameter("@Published", VersionStatusPublished),
        };

        await using var reader = await ConnectionHelper.ExecuteReaderAsync(query, parameters, QueryTypeEnum.SQLQuery, CommandBehavior.Default, cancellationToken);

        var events = new List<ContentCalendarEvent>();

        while (reader.Read())
        {
            string displayName = reader.GetString("ContentItemLanguageMetadataDisplayName");
            string classDisplayName = reader.GetString("ClassDisplayName");
            string contentTypeType = reader.IsDBNull("ClassContentTypeType") ? "" : reader.GetString("ClassContentTypeType");
            DateTime modifiedWhen = reader.GetDateTime("ContentItemLanguageMetadataModifiedWhen");
            int versionStatus = reader.GetInt32("ContentItemLanguageMetadataLatestVersionStatus");
            string workflowStepName = reader.IsDBNull("ContentWorkflowStepDisplayName") ? "" : reader.GetString("ContentWorkflowStepDisplayName");
            string workflowStepIcon = reader.IsDBNull("ContentWorkflowStepIconClass") ? "" : reader.GetString("ContentWorkflowStepIconClass");
            string workflowName = reader.IsDBNull("ContentWorkflowDisplayName") ? "" : reader.GetString("ContentWorkflowDisplayName");
            DateTime? scheduledPublish = reader.IsDBNull("ContentItemLanguageMetadataScheduledPublishWhen")
                ? null
                : reader.GetDateTime("ContentItemLanguageMetadataScheduledPublishWhen");
            DateTime? scheduledUnpublish = reader.IsDBNull("ContentItemLanguageMetadataScheduledUnpublishWhen")
                ? null
                : reader.GetDateTime("ContentItemLanguageMetadataScheduledUnpublishWhen");

            int contentItemId = reader.GetInt32("ContentItemID");
            int workspaceId = reader.IsDBNull("ContentItemWorkspaceID") ? 0 : reader.GetInt32("ContentItemWorkspaceID");
            string languageName = reader.GetString("ContentLanguageName");
            int websiteChannelId = reader.IsDBNull("WebsiteChannelID") ? 0 : reader.GetInt32("WebsiteChannelID");
            int webPageItemId = reader.IsDBNull("WebPageItemID") ? 0 : reader.GetInt32("WebPageItemID");
            int headlessChannelId = reader.IsDBNull("HeadlessChannelID") ? 0 : reader.GetInt32("HeadlessChannelID");
            int headlessItemId = reader.IsDBNull("HeadlessItemID") ? 0 : reader.GetInt32("HeadlessItemID");
            int emailChannelId = reader.IsDBNull("EmailChannelID") ? 0 : reader.GetInt32("EmailChannelID");
            int emailConfigId = reader.IsDBNull("EmailConfigurationID") ? 0 : reader.GetInt32("EmailConfigurationID");

            string editUrl = BuildEditUrl(
                contentTypeType,
                contentItemId, workspaceId, languageName,
                websiteChannelId, webPageItemId,
                headlessChannelId, headlessItemId,
                emailChannelId, emailConfigId);

            // Determine status and date
            string status;
            DateTime eventDate;

            if (scheduledPublish.HasValue)
            {
                status = "scheduled";
                eventDate = scheduledPublish.Value;
            }
            else if (versionStatus == VersionStatusPublished)
            {
                status = "published";
                eventDate = modifiedWhen;
            }
            else
            {
                status = "draft";
                eventDate = modifiedWhen;
            }

            // Only add if the determined event date falls within the requested range
            if (eventDate >= start && eventDate < end)
            {
                events.Add(new ContentCalendarEvent(
                    Title: displayName,
                    Date: eventDate,
                    ContentType: classDisplayName,
                    ContentTypeType: contentTypeType,
                    Status: status,
                    WorkflowStepName: workflowStepName,
                    WorkflowStepIcon: workflowStepIcon,
                    WorkflowName: workflowName,
                    EditUrl: editUrl));
            }

            // Separate entry for scheduled unpublish
            if (scheduledUnpublish.HasValue && scheduledUnpublish.Value >= start && scheduledUnpublish.Value < end)
            {
                events.Add(new ContentCalendarEvent(
                    Title: displayName,
                    Date: scheduledUnpublish.Value,
                    ContentType: classDisplayName,
                    ContentTypeType: contentTypeType,
                    Status: "unpublish-scheduled",
                    WorkflowStepName: workflowStepName,
                    WorkflowStepIcon: workflowStepIcon,
                    WorkflowName: workflowName,
                    EditUrl: editUrl));
            }
        }

        return [.. events];
    }

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

public class ContentCalendarPageClientProperties : TemplateClientProperties
{
    public string LoadEventsCommandName { get; set; } = "";
    public ContentCalendarEvent[] InitialEvents { get; set; } = [];
}

public record LoadEventsParams(DateTime Start, DateTime End);

public record ContentCalendarEvent(
    string Title,
    DateTime Date,
    string ContentType,
    string ContentTypeType,
    string Status,
    string WorkflowStepName,
    string WorkflowStepIcon,
    string WorkflowName,
    string EditUrl);
