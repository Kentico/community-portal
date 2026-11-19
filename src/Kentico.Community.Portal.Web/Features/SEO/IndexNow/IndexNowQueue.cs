using System.Threading.Channels;

namespace Kentico.Community.Portal.Web.Features.SEO.IndexNow;

/// <summary>
/// A published page waiting for IndexNow submission
/// </summary>
public record IndexNowPage(int WebPageItemID, string WebsiteChannelName, string LanguageName);

/// <summary>
/// In-memory hand-off from publish events to <see cref="IndexNowSubmissionService"/>.
/// Writes never block or throw, so enqueueing is safe inside a publish.
/// </summary>
/// <remarks>
/// Not durable: pages queued when the app stops are not submitted. Engines still find them through
/// the sitemap, so IndexNow only speeds discovery up.
/// </remarks>
public class IndexNowQueue
{
    /// <summary>
    /// Bounds memory if the endpoint is unreachable for a long time; the oldest pages are dropped first
    /// </summary>
    public const int CAPACITY = IndexNowOptions.MAX_URLS_PER_REQUEST;

    private readonly Channel<IndexNowPage> channel = Channel.CreateBounded<IndexNowPage>(
        new BoundedChannelOptions(CAPACITY)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

    public bool Enqueue(IndexNowPage page) => channel.Writer.TryWrite(page);

    public ChannelReader<IndexNowPage> Reader => channel.Reader;

    /// <summary>
    /// Waits for at least one page, then collects everything queued within <paramref name="window"/>, de-duplicated
    /// </summary>
    public async Task<IReadOnlyCollection<IndexNowPage>> ReadBatch(TimeSpan window, TimeProvider clock, CancellationToken cancellationToken)
    {
        var batch = new HashSet<IndexNowPage>();
        if (!await Reader.WaitToReadAsync(cancellationToken))
        {
            return batch;
        }

        if (window > TimeSpan.Zero)
        {
            await Task.Delay(window, clock, cancellationToken);
        }

        while (batch.Count < CAPACITY && Reader.TryRead(out var page))
        {
            _ = batch.Add(page);
        }

        return batch;
    }
}
