using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Features.SEO.IndexNow;

/// <summary>
/// Drains <see cref="IndexNowQueue"/> in batches and submits indexable URLs, outside any publish.
/// Errors are logged and the loop continues; failed batches are not retried (the sitemap still covers them).
/// </summary>
public class IndexNowSubmissionService(
    IndexNowQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<IndexNowOptions> options,
    TimeProvider clock,
    ILogger<IndexNowSubmissionService> logger) : BackgroundService
{
    private readonly IndexNowQueue queue = queue;
    private readonly IServiceScopeFactory scopeFactory = scopeFactory;
    private readonly IOptionsMonitor<IndexNowOptions> options = options;
    private readonly TimeProvider clock = clock;
    private readonly ILogger<IndexNowSubmissionService> logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.CurrentValue;
        if (!settings.IsEnabled)
        {
            if (!string.IsNullOrEmpty(settings.Key))
            {
                logger.LogError(IndexNowLog.Configuration, "{Setting} is invalid (8-128 characters: a-z, A-Z, 0-9, '-'). IndexNow is disabled.", $"{IndexNowOptions.SECTION}:{nameof(IndexNowOptions.Key)}");
            }
            else
            {
                logger.LogInformation(IndexNowLog.Configuration, "{Setting} is not set. IndexNow is disabled.", $"{IndexNowOptions.SECTION}:{nameof(IndexNowOptions.Key)}");
            }
        }
        else if (!settings.IsSubmissionEnabled)
        {
            logger.LogInformation(IndexNowLog.Configuration, "{Setting} is not an https URL. The key file is served, but nothing is submitted.", $"{IndexNowOptions.SECTION}:{nameof(IndexNowOptions.Endpoint)}");
        }
        else
        {
            logger.LogInformation(IndexNowLog.Configuration, "IndexNow submissions enabled, endpoint {Endpoint}", settings.Endpoint);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batch = await queue.ReadBatch(options.CurrentValue.BatchWindow, clock, stoppingToken);
                if (batch.Count > 0)
                {
                    await Submit(batch, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(IndexNowLog.Failed, ex, "IndexNow submission batch failed");
            }
        }
    }

    private async Task Submit(IReadOnlyCollection<IndexNowPage> batch, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IndexNowUrlResolver>();
        var client = scope.ServiceProvider.GetRequiredService<IndexNowClient>();

        var urls = await resolver.GetIndexableURLs(batch, cancellationToken);
        if (urls.Count == 0)
        {
            logger.LogDebug(IndexNowLog.Skipped, "No indexable URLs among {PageCount} published pages", batch.Count);
            return;
        }

        _ = await client.Submit(urls, cancellationToken);
    }
}
