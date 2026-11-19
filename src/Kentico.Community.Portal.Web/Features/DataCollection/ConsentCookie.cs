using System.Globalization;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

/// <summary>
/// A visitor's cookie choice, stored in the <see cref="CookieNames.CONSENT"/> cookie as
/// <c>v2|a=&lt;0|1&gt;|t=&lt;0|1&gt;|&lt;unix seconds&gt;</c>, plus one read-only marker:
/// <c>|ro=1</c> (<see cref="Provisional"/>) or <c>|rv=&lt;contact GUID&gt;</c> (<see cref="PendingRevoke"/>).
/// </summary>
/// <param name="Analytics">Google Analytics allowed (cookie-only record, no agreement)</param>
/// <param name="Tracking">Website tracking allowed (Xperience contact tracking, backed by a consent agreement)</param>
/// <param name="SavedAt">When the choice was made</param>
/// <param name="Provisional">Tracking was requested in read-only mode, where no agreement can be
/// recorded. Saved with tracking off; treated as no choice once read-only mode ends, so the visitor
/// is asked again.</param>
/// <param name="PendingRevoke">Tracking was withdrawn in read-only mode, where the agreement can't be
/// revoked. The contact whose Website tracking agreement is revoked once read-only mode ends.</param>
public record ConsentCookie(bool Analytics, bool Tracking, DateTimeOffset SavedAt, bool Provisional = false, Guid? PendingRevoke = null)
{
    public const string VERSION = "v2";
    private const string PROVISIONAL_MARKER = "ro=1";
    private const string PENDING_REVOKE_PREFIX = "rv=";
    private static readonly long MAX_UNIX_SECONDS = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

    public string Format()
    {
        string value = string.Join('|',
            VERSION,
            $"a={(Analytics ? 1 : 0)}",
            $"t={(Tracking ? 1 : 0)}",
            SavedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));

        return PendingRevoke is Guid contact ? $"{value}|{PENDING_REVOKE_PREFIX}{contact:D}"
            : Provisional ? $"{value}|{PROVISIONAL_MARKER}"
            : value;
    }

    /// <summary>
    /// Parses a cookie value. Returns null for a missing, malformed or older-version value,
    /// which callers treat as "no choice made". Never throws: the value is client-controlled.
    /// </summary>
    public static ConsentCookie? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] parts = value.Split('|');
        if (parts.Length is < 4 or > 5 || parts[0] != VERSION)
        {
            return null;
        }

        bool? analytics = ParseFlag(parts[1], "a");
        bool? tracking = ParseFlag(parts[2], "t");

        // NumberStyles.None rejects signs, so negative values never parse. The range check keeps a
        // tampered value from throwing in FromUnixTimeSeconds.
        if (analytics is null
            || tracking is null
            || !long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out long seconds)
            || seconds > MAX_UNIX_SECONDS)
        {
            return null;
        }

        bool provisional = false;
        Guid? pendingRevoke = null;

        if (parts.Length == 5)
        {
            if (parts[4] == PROVISIONAL_MARKER)
            {
                provisional = true;
            }
            else if (parts[4].StartsWith(PENDING_REVOKE_PREFIX, StringComparison.Ordinal)
                && Guid.TryParse(parts[4].AsSpan(PENDING_REVOKE_PREFIX.Length), out var contact))
            {
                pendingRevoke = contact;
            }
            else
            {
                return null;
            }
        }

        return new(analytics.Value, tracking.Value, DateTimeOffset.FromUnixTimeSeconds(seconds), provisional, pendingRevoke);
    }

    private static bool? ParseFlag(string part, string key) =>
        part == $"{key}=1" ? true
        : part == $"{key}=0" ? false
        : null;
}

/// <summary>
/// Names of the portal's custom cookies. Each is registered with a cookie level in
/// <c>ServiceCollectionXperienceExtensions</c>.
/// </summary>
public static class CookieNames
{
    /// <summary>
    /// The visitor's cookie choice (<see cref="ConsentCookie"/>). Registered at <c>CookieLevel.System</c>.
    /// </summary>
    public const string CONSENT = "kcp.cookieconsent";

    /// <summary>
    /// Pre-v2 consent cookies. Never written; deleted when found, which resets the visitor to Essential.
    /// </summary>
    public static readonly IReadOnlyList<string> LEGACY_CONSENT =
    [
        "kenticocommunityportal.cookieconsentlevel",
        "kenticocommunityportal.cookielevelselection",
    ];
}
