namespace Kentico.Community.Portal.Core.DataProtection;

/// <summary>
/// Values of <c>FormRetentionPolicyInfo.FormRetentionPolicyType</c>.
/// </summary>
public static class FormRetentionPolicyTypes
{
    /// <summary>
    /// Program or community history (polls, MVP/Community Leader activity). Kept while the portal
    /// runs. Erasure scrubs the email but keeps the row and member ID.
    /// </summary>
    public const string HISTORY = "History";

    /// <summary>
    /// Deleted after the policy's retention days (or <see cref="RetentionOptions.ShortLivedFormDefaultDays"/>).
    /// Erasure deletes the row.
    /// </summary>
    public const string SHORT_LIVED = "ShortLived";

    public static IReadOnlyList<string> All { get; } = [HISTORY, SHORT_LIVED];
}
