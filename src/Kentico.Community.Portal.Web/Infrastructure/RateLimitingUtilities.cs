using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CMS.Core;
using Microsoft.AspNetCore.RateLimiting;

namespace Kentico.Community.Portal.Web.Infrastructure;

/// <summary>
/// Utilities and common configurations for rate limiting
/// </summary>
public static class RateLimitingUtilities
{
    /// <summary>
    /// Standard partition key selector that uses authenticated user name or IP address as fallback.
    /// IP address translation for Cloudflare and other proxies is handled globally by ForwardedHeaders middleware.
    /// </summary>
    public static readonly Func<HttpContext, string> StandardPartitionKeySelector =
        ctx => GetMemberIDFromContext(ctx)
            .Map(id => id.ToString())
            .Match(id => id, () => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown");

    private static Maybe<int> GetMemberIDFromContext(HttpContext ctx) =>
        (ctx.User.Identity is not ClaimsIdentity claimsIdentity
            || !claimsIdentity.IsAuthenticated
            || claimsIdentity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) is not Claim idClaim
            || !int.TryParse(idClaim.Value, out int id))
            ? Maybe<int>.None
            : Maybe.From(id);

    /// <summary>
    /// Creates a fixed window rate limiter partition with standard per-user partition key.
    /// This method provides consistent rate limiting behavior across the application by using
    /// the authenticated user's name as the partition key, falling back to IP address for anonymous users.
    /// </summary>
    /// <param name="httpContext">The current HTTP context containing user and connection information</param>
    /// <param name="permitLimit">The maximum number of requests allowed within the time window</param>
    /// <param name="window">The time window during which the permit limit applies (e.g., TimeSpan.FromMinutes(15))</param>
    /// <returns>A configured rate limit partition that uses fixed window limiting with automatic replenishment</returns>
    /// <remarks>
    /// <para>The partition key is determined by <see cref="StandardPartitionKeySelector"/> which uses:</para>
    /// <list type="number">
    /// <item>Authenticated user name (preferred)</item>
    /// <item>Client IP address (fallback for anonymous users)</item>
    /// <item>"unknown" (fallback when IP is unavailable)</item>
    /// </list>
    /// <para>The fixed window resets completely when the time window expires, allowing full permit limit again.</para>
    /// <para>Auto-replenishment is enabled, so the window automatically resets without manual intervention.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Allow 5 requests per 15 minutes
    /// var partition = CreateFixedWindowPartition(httpContext, 
    ///     permitLimit: 5, 
    ///     window: TimeSpan.FromMinutes(15));
    /// 
    /// // Allow 10 requests per 5 minutes
    /// var partition = CreateFixedWindowPartition(httpContext, 
    ///     permitLimit: 10, 
    ///     window: TimeSpan.FromMinutes(5));
    /// </code>
    /// </example>
    public static RateLimitPartition<string> CreateFixedWindowPartition(
        HttpContext httpContext,
        int permitLimit,
        TimeSpan window) => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: StandardPartitionKeySelector(httpContext),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window
            });

    public static RateLimitPartition<string> CreateSlidingWindowPartition(
        HttpContext httpContext,
        int permitLimit,
        int segmentsPerWindow,
        TimeSpan window) => RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: StandardPartitionKeySelector(httpContext),
            factory: partition => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                SegmentsPerWindow = segmentsPerWindow,
            });

    /// <summary>
    /// Standard OnRejected handler for rate limiting that works with HTMX requests
    /// </summary>
    public static readonly Func<OnRejectedContext, CancellationToken, ValueTask> OnRejectedHandler =
        async (context, token) =>
        {
            var httpContext = context.HttpContext;
            httpContext.Response.StatusCode = 429; // Too Many Requests

            LogEvent(context);

            string message = "Rate limit exceeded. Please wait a few minutes and try again.";
            bool isHtmxRequest = httpContext.Request.Headers.ContainsKey("HX-Request");
            if (!isHtmxRequest)
            {
                // Return plain text for non-HTMX requests
                httpContext.Response.ContentType = "text/plain";
                await httpContext.Response.WriteAsync(message, cancellationToken: token);
                return;
            }

            // Return JSON using proper serialization
            var errorResponse = new ErrorResponse
            {
                Detail = new ErrorDetail
                {
                    Message = message,
                    Status = "failure"
                }
            };

            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken: token);
        };

    private static void LogEvent(OnRejectedContext context)
    {
        var httpContext = context.HttpContext;
        var log = httpContext.RequestServices.GetRequiredService<IEventLogService>()!;
        string requester = StandardPartitionKeySelector(httpContext);
        var sb = new StringBuilder($"Rate limit exceeded.");
        _ = sb
            .AppendLine($"Requester: {requester}.");
        foreach (var (metaKey, metaValue) in context.Lease.GetAllMetadata())
        {
            _ = sb.AppendLine($"{metaKey}: {metaValue}");
        }

        // Ensure eventCode stays within 100 character database limit
        const int maxEventCodeLength = 30;
        const string prefix = "RATE_LIMIT_EXCEEDED_";
        int maxRequesterLength = maxEventCodeLength - prefix.Length;

        string eventCode = requester.Length <= maxRequesterLength
            ? $"{prefix}{requester}"
            : $"{prefix}{requester[..maxRequesterLength]}";

        log.LogWarning(
            nameof(RateLimitingUtilities),
            eventCode,
            sb.ToString(),
            new LoggingPolicy(TimeSpan.FromMinutes(10)));

    }
}
