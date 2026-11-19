using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Features.SEO.IndexNow;

/// <summary>
/// Submits URLs to the IndexNow endpoint (https://www.indexnow.org/documentation), one request per host
/// </summary>
public class IndexNowClient(
    HttpClient httpClient,
    IOptionsMonitor<IndexNowOptions> options,
    ILogger<IndexNowClient> logger)
{
    private readonly HttpClient httpClient = httpClient;
    private readonly IOptionsMonitor<IndexNowOptions> options = options;
    private readonly ILogger<IndexNowClient> logger = logger;

    /// <returns>Failure when any host's submission failed. Failures are also logged.</returns>
    public async Task<Result> Submit(IReadOnlyCollection<string> urls, CancellationToken cancellationToken)
    {
        var settings = options.CurrentValue;
        if (!settings.IsSubmissionEnabled)
        {
            return Result.Failure("IndexNow is not configured");
        }

        var errors = new List<string>();
        foreach (var request in IndexNowRequest.Create(settings.Key, urls))
        {
            string? error = await Send(settings, request, cancellationToken);
            if (error is not null)
            {
                errors.Add(error);
            }
        }

        return errors.Count == 0
            ? Result.Success()
            : Result.Failure(string.Join("; ", errors));
    }

    private async Task<string?> Send(IndexNowOptions settings, IndexNowRequest request, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.RequestTimeout);

        try
        {
            using var response = await httpClient.PostAsJsonAsync(settings.Endpoint, request, timeout.Token);

            // 200 OK, or 202 Accepted while the engine verifies the key file
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(IndexNowLog.Submitted,
                    "IndexNow accepted {URLCount} URLs for {Host} ({StatusCode}): {URLs}",
                    request.UrlList.Count, request.Host, (int)response.StatusCode, request.UrlList);
                return null;
            }

            string body = await response.Content.ReadAsStringAsync(timeout.Token);
            logger.LogWarning(IndexNowLog.Failed,
                "IndexNow rejected {URLCount} URLs for {Host} ({StatusCode}): {Body}. URLs: {URLs}",
                request.UrlList.Count, request.Host, (int)response.StatusCode, body, request.UrlList);
            return $"{request.Host}: {(int)response.StatusCode}";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(IndexNowLog.Failed,
                "IndexNow request for {Host} timed out after {Timeout}. URLs: {URLs}",
                request.Host, settings.RequestTimeout, request.UrlList);
            return $"{request.Host}: timeout";
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(IndexNowLog.Failed, ex,
                "IndexNow endpoint unavailable for {Host}. URLs: {URLs}",
                request.Host, request.UrlList);
            return $"{request.Host}: {ex.Message}";
        }
    }
}

/// <summary>
/// IndexNow JSON request body. All URLs in one request share the host.
/// </summary>
public record IndexNowRequest(
    [property: JsonPropertyName("host")] string Host,
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("keyLocation")] string KeyLocation,
    [property: JsonPropertyName("urlList")] IReadOnlyList<string> UrlList)
{
    /// <summary>
    /// Groups <paramref name="urls"/> by host (and scheme, for the key location), de-duplicated,
    /// split at the protocol's per-request limit. Only http(s) URLs are kept; on Unix a rooted
    /// path like <c>/page</c> parses as an absolute <c>file://</c> URI, so the scheme is checked
    /// </summary>
    public static IEnumerable<IndexNowRequest> Create(string key, IEnumerable<string> urls) =>
        urls
            .Select(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) ? uri : null)
            .OfType<Uri>()
            .Where(u => u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp)
            .DistinctBy(u => u.AbsoluteUri)
            .GroupBy(u => (u.Scheme, Authority: u.Authority))
            .SelectMany(g => g
                .Select(u => u.AbsoluteUri)
                .Chunk(IndexNowOptions.MAX_URLS_PER_REQUEST)
                .Select(chunk => new IndexNowRequest(
                    g.First().Host,
                    key,
                    $"{g.Key.Scheme}://{g.Key.Authority}/{key}.txt",
                    chunk)));
}
