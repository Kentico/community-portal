using CMS.Base;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Features.SEO.IndexNow;

/// <summary>
/// Queues every published website channel page for IndexNow submission.
/// Never does I/O and never throws: submission happens in <see cref="IndexNowSubmissionService"/>,
/// outside the publish, so a slow or failing endpoint cannot affect publishing.
/// </summary>
public class IndexNowPublishHandler(
    IndexNowQueue queue,
    IOptionsMonitor<IndexNowOptions> options,
    ILogger<IndexNowPublishHandler> logger) : IAsyncEventHandler<AfterPublishWebPageEvent>
{
    private readonly IndexNowQueue queue = queue;
    private readonly IOptionsMonitor<IndexNowOptions> options = options;
    private readonly ILogger<IndexNowPublishHandler> logger = logger;

    public Task HandleAsync(AfterPublishWebPageEvent asyncEvent, CancellationToken cancellationToken)
    {
        _ = Handle(asyncEvent.Data);

        return Task.CompletedTask;
    }

    /// <returns>True when the page was queued</returns>
    public bool Handle(PublishWebPageEventData data)
    {
        try
        {
            if (!options.CurrentValue.IsSubmissionEnabled)
            {
                return false;
            }

            if (queue.Enqueue(new(data.ID, data.WebsiteChannelName, data.ContentLanguageName)))
            {
                return true;
            }

            logger.LogWarning(IndexNowLog.Queue, "Could not queue page {WebPageItemID} for IndexNow submission", data.ID);
        }
        catch (Exception ex)
        {
            logger.LogError(IndexNowLog.Queue, ex, "Could not queue a published page for IndexNow submission");
        }

        return false;
    }
}
public static class IndexNowLog
{
    public static readonly EventId Queue = new(0, "INDEXNOW_QUEUE");
    public static readonly EventId Submitted = new(0, "INDEXNOW_SUBMITTED");
    public static readonly EventId Failed = new(0, "INDEXNOW_FAILED");
    public static readonly EventId Skipped = new(0, "INDEXNOW_SKIPPED");
    public static readonly EventId Configuration = new(0, "INDEXNOW_CONFIGURATION");
}
