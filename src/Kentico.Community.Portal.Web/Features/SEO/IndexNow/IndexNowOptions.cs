using System.Text.RegularExpressions;

namespace Kentico.Community.Portal.Web.Features.SEO.IndexNow;

/// <summary>
/// IndexNow (https://www.indexnow.org) settings, bound from the <see cref="SECTION"/> app settings section.
/// Submission is off until a valid <see cref="Key"/> is set.
/// </summary>
public partial class IndexNowOptions
{
    public const string SECTION = "IndexNow";

    /// <summary>
    /// Shared endpoint: participating engines (Bing, Yandex, Seznam, Naver, Yep, ...) share submissions
    /// </summary>
    public const string DEFAULT_ENDPOINT = "https://api.indexnow.org/indexnow";

    /// <summary>
    /// Protocol limit per request
    /// </summary>
    public const int MAX_URLS_PER_REQUEST = 10_000;

    /// <summary>
    /// Site verification key, served at <c>/{Key}.txt</c>. 8-128 characters: a-z, A-Z, 0-9, '-'.
    /// Empty disables IndexNow (the default outside production).
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// Empty serves the key file but submits nothing (Development, CI)
    /// </summary>
    public string Endpoint { get; set; } = DEFAULT_ENDPOINT;

    /// <summary>
    /// How long to collect published pages before one batched submission
    /// </summary>
    public TimeSpan BatchWindow { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Per-request timeout for the IndexNow endpoint
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The key file is served
    /// </summary>
    public bool IsEnabled => IsValidKey(Key);

    /// <summary>
    /// Published pages are queued and submitted
    /// </summary>
    public bool IsSubmissionEnabled =>
        IsEnabled
        && Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;

    public static bool IsValidKey(string? key) => key is not null && KeyPattern().IsMatch(key);

    [GeneratedRegex("^[a-zA-Z0-9-]{8,128}$")]
    private static partial Regex KeyPattern();
}
