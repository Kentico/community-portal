using System.Net;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Retries idempotent requests on transient failures (network errors, 408, 429, 5xx)
/// with exponential backoff and jitter. Dependency-free replacement for Polly-based resilience.
/// </summary>
public sealed class TransientRetryHandler : DelegatingHandler
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryAfter = TimeSpan.FromSeconds(30);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!IsIdempotent(request.Method))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        for (int attempt = 0; ; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException) when (attempt < MaxRetries)
            {
                await Task.Delay(Backoff(attempt), cancellationToken);
                continue;
            }

            if (attempt >= MaxRetries || !IsTransient(response.StatusCode))
            {
                return response;
            }

            var delay = RetryAfter(response) ?? Backoff(attempt);
            response.Dispose();
            await Task.Delay(delay, cancellationToken);
        }
    }

    private static bool IsIdempotent(HttpMethod method) =>
        method == HttpMethod.Get
        || method == HttpMethod.Head
        || method == HttpMethod.Options
        || method == HttpMethod.Put
        || method == HttpMethod.Delete;

    private static bool IsTransient(HttpStatusCode code) =>
        code is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
        || (int)code >= 500;

    private static TimeSpan Backoff(int attempt) =>
        (BaseDelay * Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250));

    private static TimeSpan? RetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        var delay = retryAfter?.Delta
            ?? (retryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : null);

        return delay is { } d && d > TimeSpan.Zero && d <= MaxRetryAfter ? d : null;
    }
}
